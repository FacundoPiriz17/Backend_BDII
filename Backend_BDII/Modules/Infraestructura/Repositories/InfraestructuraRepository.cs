using System.Text;
using Backend_BDII.Common.Database;
using Backend_BDII.Common.Domain;
using Backend_BDII.Modules.Infraestructura.DTOs;
using Npgsql;

namespace Backend_BDII.Modules.Infraestructura.Repositories;

public sealed class InfraestructuraRepository : IInfraestructuraRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public InfraestructuraRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<EstadioResponse>> GetEstadiosAsync(string? pais, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var sql = new StringBuilder(BaseEstadiosSql);
        sql.AppendLine();

        if (!string.IsNullOrWhiteSpace(pais))
            sql.AppendLine("WHERE e.pais::text = @pais");

        sql.AppendLine("ORDER BY e.pais, e.nombre_estadio, s.nombre_sector;");

        await using var command = new NpgsqlCommand(sql.ToString(), connection);

        if (!string.IsNullOrWhiteSpace(pais))
            command.Parameters.AddWithValue("pais", PaisSedeNormalizer.Normalize(pais));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await MapEstadiosAsync(reader, cancellationToken);
    }

    public async Task<EstadioResponse?> GetEstadioByIdAsync(int idEstadio, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        return await GetEstadioByIdUsingConnectionAsync(connection, idEstadio, null, cancellationToken);
    }

    public async Task<EstadioResponse> CrearEstadioAsync(
        string emailAdmin,
        CrearEstadioRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var pais = PaisSedeNormalizer.Normalize(request.Pais);
            await ValidarPaisAdminAsync(connection, transaction, emailAdmin, pais, cancellationToken);

            const string insertEstadioSql = """
                INSERT INTO estadio (
                    nombre_estadio,
                    capacidad,
                    ubicacion,
                    ciudad,
                    pais
                )
                VALUES (
                    @nombre,
                    @capacidad,
                    @ubicacion,
                    @ciudad,
                    CAST(@pais AS pais_sede_enum)
                )
                RETURNING id_estadio;
                """;

            int idEstadio;

            await using (var command = new NpgsqlCommand(insertEstadioSql, connection, transaction))
            {
                command.Parameters.AddWithValue("nombre", request.Nombre.Trim());
                command.Parameters.AddWithValue("capacidad", (object?)request.Capacidad ?? DBNull.Value);
                command.Parameters.AddWithValue("ubicacion", (object?)request.Ubicacion?.Trim() ?? DBNull.Value);
                command.Parameters.AddWithValue("ciudad", (object?)request.Ciudad?.Trim() ?? DBNull.Value);
                command.Parameters.AddWithValue("pais", pais);

                idEstadio = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            }

            const string insertSectorSql = """
                INSERT INTO sector (nombre_sector, capacidad, id_estadio, costo)
                VALUES (CAST(@nombre_sector AS sector_enum), @capacidad, @id_estadio, @costo);
                """;

            foreach (var sector in request.Sectores)
            {
                await using var command = new NpgsqlCommand(insertSectorSql, connection, transaction);
                command.Parameters.AddWithValue("nombre_sector", sector.NombreSector.Trim().ToUpperInvariant());
                command.Parameters.AddWithValue("capacidad", (object?)sector.Capacidad ?? DBNull.Value);
                command.Parameters.AddWithValue("id_estadio", idEstadio);
                command.Parameters.AddWithValue("costo", (object?)sector.Costo ?? DBNull.Value);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            var estadio = await GetEstadioByIdUsingConnectionAsync(connection, idEstadio, transaction, cancellationToken)
                          ?? throw new InvalidOperationException("El estadio fue creado, pero no se pudo recuperar.");

            await transaction.CommitAsync(cancellationToken);
            return estadio;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<EstadioResponse?> ActualizarEstadioAsync(
        int idEstadio,
        string emailAdmin,
        ActualizarEstadioRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var pais = PaisSedeNormalizer.Normalize(request.Pais);
            await ValidarPaisAdminAsync(connection, transaction, emailAdmin, pais, cancellationToken);

            const string updateSql = """
                UPDATE estadio
                SET nombre_estadio = @nombre,
                    capacidad = @capacidad,
                    ubicacion = @ubicacion,
                    ciudad = @ciudad,
                    pais = CAST(@pais AS pais_sede_enum)
                WHERE id_estadio = @id_estadio;
                """;

            int affectedRows;

            await using (var command = new NpgsqlCommand(updateSql, connection, transaction))
            {
                command.Parameters.AddWithValue("id_estadio", idEstadio);
                command.Parameters.AddWithValue("nombre", request.Nombre.Trim());
                command.Parameters.AddWithValue("capacidad", (object?)request.Capacidad ?? DBNull.Value);
                command.Parameters.AddWithValue("ubicacion", (object?)request.Ubicacion?.Trim() ?? DBNull.Value);
                command.Parameters.AddWithValue("ciudad", (object?)request.Ciudad?.Trim() ?? DBNull.Value);
                command.Parameters.AddWithValue("pais", pais);

                affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
            }

            if (affectedRows == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var estadio = await GetEstadioByIdUsingConnectionAsync(connection, idEstadio, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return estadio;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<SectorInfraestructuraResponse?> ActualizarSectorAsync(
        int idEstadio,
        string nombreSector,
        string emailAdmin,
        ActualizarSectorRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var paisEstadio = await GetPaisEstadioAsync(connection, transaction, idEstadio, cancellationToken);

            if (paisEstadio is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            await ValidarPaisAdminAsync(connection, transaction, emailAdmin, paisEstadio, cancellationToken);

            const string updateSql = """
                UPDATE sector
                SET capacidad = @capacidad,
                    costo = @costo
                WHERE id_estadio = @id_estadio
                  AND nombre_sector = CAST(@nombre_sector AS sector_enum)
                RETURNING nombre_sector::text, capacidad AS capacidad_sector, costo;
                """;

            await using var command = new NpgsqlCommand(updateSql, connection, transaction);
            command.Parameters.AddWithValue("id_estadio", idEstadio);
            command.Parameters.AddWithValue("nombre_sector", nombreSector.Trim().ToUpperInvariant());
            command.Parameters.AddWithValue("capacidad", (object?)request.Capacidad ?? DBNull.Value);
            command.Parameters.AddWithValue("costo", (object?)request.Costo ?? DBNull.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var sector = MapSector(reader);
            await reader.CloseAsync();
            await transaction.CommitAsync(cancellationToken);

            return sector;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<List<EquipoResponse>> GetEquiposAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT codigo_fifa, nombre_equipo, grupo
            FROM equipo
            ORDER BY grupo, nombre_equipo;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var equipos = new List<EquipoResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            equipos.Add(new EquipoResponse
            {
                CodigoFifa = reader.GetString(reader.GetOrdinal("codigo_fifa")),
                Nombre = reader.GetString(reader.GetOrdinal("nombre_equipo")),
                Grupo = reader.GetString(reader.GetOrdinal("grupo"))
            });
        }

        return equipos;
    }

    public async Task<List<DispositivoResponse>> GetDispositivosAsync(
        string? emailFuncionario,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var sql = new StringBuilder(BaseDispositivosSql);
        sql.AppendLine();

        if (!string.IsNullOrWhiteSpace(emailFuncionario))
            sql.AppendLine("WHERE LOWER(d.email_funcionario) = LOWER(@email_funcionario)");

        sql.AppendLine("ORDER BY d.id_dispositivo_escaneo;");

        await using var command = new NpgsqlCommand(sql.ToString(), connection);

        if (!string.IsNullOrWhiteSpace(emailFuncionario))
            command.Parameters.AddWithValue("email_funcionario", emailFuncionario);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var dispositivos = new List<DispositivoResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            dispositivos.Add(MapDispositivo(reader));
        }

        return dispositivos;
    }

    public async Task<DispositivoResponse?> GetDispositivoByIdAsync(int idDispositivo, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        return await GetDispositivoByIdUsingConnectionAsync(connection, idDispositivo, null, cancellationToken);
    }

    public async Task<DispositivoResponse> CrearDispositivoAsync(
        CrearDispositivoRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            INSERT INTO dispositivo_escaneo (modelo, activo, email_funcionario)
            VALUES (@modelo, @activo, @email_funcionario)
            RETURNING id_dispositivo_escaneo;
            """;

        int idDispositivo;

        await using (var command = new NpgsqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("modelo", (object?)request.Modelo?.Trim() ?? DBNull.Value);
            command.Parameters.AddWithValue("activo", request.Activo);
            command.Parameters.AddWithValue("email_funcionario", request.EmailFuncionario.Trim().ToLowerInvariant());

            idDispositivo = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        }

        return await GetDispositivoByIdUsingConnectionAsync(connection, idDispositivo, null, cancellationToken)
               ?? throw new InvalidOperationException("El dispositivo fue creado, pero no se pudo recuperar.");
    }

    public async Task<DispositivoResponse?> ActualizarDispositivoAsync(
        int idDispositivo,
        ActualizarDispositivoRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            UPDATE dispositivo_escaneo
            SET modelo = @modelo,
                activo = @activo,
                email_funcionario = @email_funcionario
            WHERE id_dispositivo_escaneo = @id_dispositivo
            RETURNING id_dispositivo_escaneo;
            """;

        int? idActualizado;

        await using (var command = new NpgsqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("id_dispositivo", idDispositivo);
            command.Parameters.AddWithValue("modelo", (object?)request.Modelo?.Trim() ?? DBNull.Value);
            command.Parameters.AddWithValue("activo", request.Activo);
            command.Parameters.AddWithValue("email_funcionario", request.EmailFuncionario.Trim().ToLowerInvariant());

            var result = await command.ExecuteScalarAsync(cancellationToken);
            idActualizado = result is null ? null : Convert.ToInt32(result);
        }

        if (idActualizado is null)
            return null;

        return await GetDispositivoByIdUsingConnectionAsync(connection, idDispositivo, null, cancellationToken);
    }

    private static async Task<EstadioResponse?> GetEstadioByIdUsingConnectionAsync(
        NpgsqlConnection connection,
        int idEstadio,
        NpgsqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        var sql = BaseEstadiosSql + "\n" + """
            WHERE e.id_estadio = @id_estadio
            ORDER BY s.nombre_sector;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id_estadio", idEstadio);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var estadios = await MapEstadiosAsync(reader, cancellationToken);

        return estadios.FirstOrDefault();
    }

    private static async Task<DispositivoResponse?> GetDispositivoByIdUsingConnectionAsync(
        NpgsqlConnection connection,
        int idDispositivo,
        NpgsqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        var sql = BaseDispositivosSql + "\n" + """
            WHERE d.id_dispositivo_escaneo = @id_dispositivo;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id_dispositivo", idDispositivo);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return MapDispositivo(reader);
    }

    private static async Task ValidarPaisAdminAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string emailAdmin,
        string paisEsperado,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT pais::text
            FROM admin
            WHERE LOWER(email_admin) = LOWER(@email_admin);
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("email_admin", emailAdmin);

        var paisAdmin = await command.ExecuteScalarAsync(cancellationToken) as string;

        if (paisAdmin is null)
            throw new InvalidOperationException("El administrador no existe.");

        if (!string.Equals(paisAdmin, PaisSedeNormalizer.Normalize(paisEsperado), StringComparison.Ordinal))
            throw new InvalidOperationException("El administrador solo puede gestionar infraestructura de su pais sede.");
    }

    private static async Task<string?> GetPaisEstadioAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int idEstadio,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT pais::text
            FROM estadio
            WHERE id_estadio = @id_estadio;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id_estadio", idEstadio);

        return await command.ExecuteScalarAsync(cancellationToken) as string;
    }

    private const string BaseEstadiosSql = """
        SELECT
            e.id_estadio,
            e.nombre_estadio,
            e.capacidad AS capacidad_estadio,
            e.ubicacion,
            e.ciudad,
            e.pais::text AS pais_estadio,
            s.nombre_sector::text AS nombre_sector,
            s.capacidad AS capacidad_sector,
            s.costo
        FROM estadio e
        LEFT JOIN sector s ON s.id_estadio = e.id_estadio
        """;

    private const string BaseDispositivosSql = """
        SELECT
            d.id_dispositivo_escaneo,
            d.modelo,
            d.activo,
            d.email_funcionario,
            f.numero_legajo,
            u.nombre AS nombre_funcionario
        FROM dispositivo_escaneo d
        INNER JOIN funcionario f ON f.email_funcionario = d.email_funcionario
        INNER JOIN usuario u ON u.email = f.email_funcionario
        """;

    private static async Task<List<EstadioResponse>> MapEstadiosAsync(
        NpgsqlDataReader reader,
        CancellationToken cancellationToken)
    {
        var estadios = new Dictionary<int, EstadioResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var idEstadio = reader.GetInt32(reader.GetOrdinal("id_estadio"));

            if (!estadios.TryGetValue(idEstadio, out var estadio))
            {
                estadio = new EstadioResponse
                {
                    IdEstadio = idEstadio,
                    Nombre = reader.GetString(reader.GetOrdinal("nombre_estadio")),
                    Capacidad = reader.IsDBNull(reader.GetOrdinal("capacidad_estadio"))
                        ? null
                        : reader.GetInt32(reader.GetOrdinal("capacidad_estadio")),
                    Ubicacion = reader.IsDBNull(reader.GetOrdinal("ubicacion"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("ubicacion")),
                    Ciudad = reader.IsDBNull(reader.GetOrdinal("ciudad"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("ciudad")),
                    Pais = reader.GetString(reader.GetOrdinal("pais_estadio")),
                    Sectores = []
                };

                estadios.Add(idEstadio, estadio);
            }

            if (!reader.IsDBNull(reader.GetOrdinal("nombre_sector")))
                estadio.Sectores.Add(MapSector(reader));
        }

        return estadios.Values.ToList();
    }

    private static SectorInfraestructuraResponse MapSector(NpgsqlDataReader reader)
    {
        return new SectorInfraestructuraResponse
        {
            NombreSector = reader.GetString(reader.GetOrdinal("nombre_sector")),
            Capacidad = reader.IsDBNull(reader.GetOrdinal("capacidad_sector"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("capacidad_sector")),
            Costo = reader.IsDBNull(reader.GetOrdinal("costo"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("costo"))
        };
    }

    private static DispositivoResponse MapDispositivo(NpgsqlDataReader reader)
    {
        return new DispositivoResponse
        {
            IdDispositivoEscaneo = reader.GetInt32(reader.GetOrdinal("id_dispositivo_escaneo")),
            Modelo = reader.IsDBNull(reader.GetOrdinal("modelo"))
                ? null
                : reader.GetString(reader.GetOrdinal("modelo")),
            Activo = reader.GetBoolean(reader.GetOrdinal("activo")),
            EmailFuncionario = reader.GetString(reader.GetOrdinal("email_funcionario")),
            NumeroLegajo = reader.GetInt32(reader.GetOrdinal("numero_legajo")),
            NombreFuncionario = reader.GetString(reader.GetOrdinal("nombre_funcionario"))
        };
    }
}
