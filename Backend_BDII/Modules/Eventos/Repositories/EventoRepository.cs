using System.Text;
using Backend_BDII.Common.Database;
using Backend_BDII.Common.Domain;
using Backend_BDII.Modules.Eventos.DTOs;
using Npgsql;

namespace Backend_BDII.Modules.Eventos.Repositories;

public sealed class EventoRepository : IEventoRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EventoRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<EventoResponse>> GetEventosAsync(
        string? pais,
        string? estado,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var sql = new StringBuilder(BaseEventosSql);
        sql.AppendLine();
        sql.AppendLine("WHERE 1 = 1");

        if (!string.IsNullOrWhiteSpace(pais))
            sql.AppendLine("AND est.pais::text = @pais");

        if (!string.IsNullOrWhiteSpace(estado))
            sql.AppendLine("AND p.estado = CAST(@estado AS estado_partido_enum)");

        sql.AppendLine(GroupBySql);
        sql.AppendLine("ORDER BY p.fecha, p.hora, p.id_partido, ps.nombre_sector;");

        await using var command = new NpgsqlCommand(sql.ToString(), connection);

        if (!string.IsNullOrWhiteSpace(pais))
            command.Parameters.AddWithValue("pais", PaisSedeNormalizer.Normalize(pais));

        if (!string.IsNullOrWhiteSpace(estado))
            command.Parameters.AddWithValue("estado", estado.Trim());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await MapEventosAsync(reader, cancellationToken);
    }

    public async Task<EventoResponse?> GetByIdAsync(int idPartido, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        return await GetByIdUsingConnectionAsync(connection, idPartido, null, cancellationToken);
    }

    public async Task<EventoResponse> CrearAsync(
        string emailAdmin,
        CrearEventoRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string insertSql = """
                INSERT INTO partido (
                    fecha,
                    hora,
                    id_estadio,
                    equipo_visitante,
                    equipo_local,
                    costo,
                    fase,
                    email_admin,
                    fecha_habilitacion
                )
                VALUES (
                    @fecha,
                    @hora,
                    @id_estadio,
                    @equipo_visitante,
                    @equipo_local,
                    @costo,
                    CAST(@fase AS fase_enum),
                    @email_admin,
                    COALESCE(CAST(@fecha_habilitacion AS DATE), CURRENT_DATE)
                )
                RETURNING id_partido;
                """;

            int idPartido;

            await using (var command = new NpgsqlCommand(insertSql, connection, transaction))
            {
                command.Parameters.AddWithValue("fecha", request.Fecha);
                command.Parameters.AddWithValue("hora", request.Hora);
                command.Parameters.AddWithValue("id_estadio", request.IdEstadio);
                command.Parameters.AddWithValue("equipo_visitante", request.EquipoVisitante.Trim().ToUpperInvariant());
                command.Parameters.AddWithValue("equipo_local", request.EquipoLocal.Trim().ToUpperInvariant());
                command.Parameters.AddWithValue("costo", request.Costo);
                command.Parameters.AddWithValue("fase", request.Fase.Trim());
                command.Parameters.AddWithValue("email_admin", emailAdmin);
                command.Parameters.AddWithValue("fecha_habilitacion", (object?)request.FechaHabilitacion ?? DBNull.Value);

                idPartido = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            }

            await InsertarSectoresHabilitadosAsync(
                connection,
                transaction,
                idPartido,
                request.IdEstadio,
                request.SectoresHabilitados,
                cancellationToken);

            var evento = await GetByIdUsingConnectionAsync(connection, idPartido, transaction, cancellationToken)
                         ?? throw new InvalidOperationException("El evento fue creado, pero no se pudo recuperar.");

            await transaction.CommitAsync(cancellationToken);
            return evento;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<EventoResponse?> ActualizarAsync(
        int idPartido,
        string emailAdmin,
        ActualizarEventoRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // partido_sector referencia partido(id_partido, id_estadio); si cambia el estadio
            // hay que borrar primero los sectores habilitados para no violar la FK.
            const string deleteSectoresSql = """
                DELETE FROM partido_sector
                WHERE id_partido = @id_partido;
                """;

            await using (var command = new NpgsqlCommand(deleteSectoresSql, connection, transaction))
            {
                command.Parameters.AddWithValue("id_partido", idPartido);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            const string updateSql = """
                UPDATE partido
                SET fecha = @fecha,
                    hora = @hora,
                    id_estadio = @id_estadio,
                    equipo_visitante = @equipo_visitante,
                    equipo_local = @equipo_local,
                    marcador_local = @marcador_local,
                    marcador_visitante = @marcador_visitante,
                    costo = @costo,
                    fase = CAST(@fase AS fase_enum),
                    estado = CAST(@estado AS estado_partido_enum),
                    email_admin = @email_admin,
                    fecha_habilitacion = @fecha_habilitacion
                WHERE id_partido = @id_partido
                RETURNING id_partido;
                """;

            int? idActualizado;

            await using (var command = new NpgsqlCommand(updateSql, connection, transaction))
            {
                command.Parameters.AddWithValue("id_partido", idPartido);
                command.Parameters.AddWithValue("fecha", request.Fecha);
                command.Parameters.AddWithValue("hora", request.Hora);
                command.Parameters.AddWithValue("id_estadio", request.IdEstadio);
                command.Parameters.AddWithValue("equipo_visitante", request.EquipoVisitante.Trim().ToUpperInvariant());
                command.Parameters.AddWithValue("equipo_local", request.EquipoLocal.Trim().ToUpperInvariant());
                command.Parameters.AddWithValue("marcador_local", request.MarcadorLocal);
                command.Parameters.AddWithValue("marcador_visitante", request.MarcadorVisitante);
                command.Parameters.AddWithValue("costo", request.Costo);
                command.Parameters.AddWithValue("fase", request.Fase.Trim());
                command.Parameters.AddWithValue("estado", request.Estado.Trim());
                command.Parameters.AddWithValue("email_admin", emailAdmin);
                command.Parameters.AddWithValue("fecha_habilitacion", request.FechaHabilitacion);

                var result = await command.ExecuteScalarAsync(cancellationToken);
                idActualizado = result is null ? null : Convert.ToInt32(result);
            }

            if (idActualizado is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            await InsertarSectoresHabilitadosAsync(
                connection,
                transaction,
                idPartido,
                request.IdEstadio,
                request.SectoresHabilitados,
                cancellationToken);

            var evento = await GetByIdUsingConnectionAsync(connection, idPartido, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return evento;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<EventoResponse?> CambiarEstadoAsync(
        int idPartido,
        string emailAdmin,
        string nuevoEstado,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var paisPartido = await GetPaisEstadioPartidoAsync(connection, transaction, idPartido, cancellationToken);

            if (paisPartido is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            await ValidarPaisAdminAsync(connection, transaction, emailAdmin, paisPartido, cancellationToken);

            const string updateSql = """
                UPDATE partido
                SET estado = CAST(@estado AS estado_partido_enum)
                WHERE id_partido = @id_partido
                RETURNING id_partido;
                """;

            int? idActualizado;

            await using (var command = new NpgsqlCommand(updateSql, connection, transaction))
            {
                command.Parameters.AddWithValue("id_partido", idPartido);
                command.Parameters.AddWithValue("estado", nuevoEstado.Trim());

                var result = await command.ExecuteScalarAsync(cancellationToken);
                idActualizado = result is null ? null : Convert.ToInt32(result);
            }

            if (idActualizado is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var evento = await GetByIdUsingConnectionAsync(connection, idPartido, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return evento;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task InsertarSectoresHabilitadosAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int idPartido,
        int idEstadio,
        IEnumerable<string> sectores,
        CancellationToken cancellationToken)
    {
        const string insertSql = """
            INSERT INTO partido_sector (id_partido, nombre_sector, id_estadio)
            VALUES (@id_partido, CAST(@nombre_sector AS sector_enum), @id_estadio);
            """;

        foreach (var sector in sectores.Select(s => s.Trim().ToUpperInvariant()).Distinct())
        {
            await using var command = new NpgsqlCommand(insertSql, connection, transaction);
            command.Parameters.AddWithValue("id_partido", idPartido);
            command.Parameters.AddWithValue("nombre_sector", sector);
            command.Parameters.AddWithValue("id_estadio", idEstadio);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task<EventoResponse?> GetByIdUsingConnectionAsync(
        NpgsqlConnection connection,
        int idPartido,
        NpgsqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        var sql = BaseEventosSql + "\n" + "WHERE p.id_partido = @id_partido\n" + GroupBySql + "\nORDER BY ps.nombre_sector;";

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id_partido", idPartido);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var eventos = await MapEventosAsync(reader, cancellationToken);

        return eventos.FirstOrDefault();
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

        if (!string.Equals(paisAdmin, paisEsperado, StringComparison.Ordinal))
            throw new InvalidOperationException("El administrador solo puede gestionar eventos de su pais sede.");
    }

    private static async Task<string?> GetPaisEstadioPartidoAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int idPartido,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT est.pais::text
            FROM partido p
            INNER JOIN estadio est ON est.id_estadio = p.id_estadio
            WHERE p.id_partido = @id_partido;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id_partido", idPartido);

        return await command.ExecuteScalarAsync(cancellationToken) as string;
    }

    private const string BaseEventosSql = """
        SELECT
            p.id_partido,
            p.fecha,
            p.hora,
            p.equipo_local,
            p.equipo_visitante,
            p.marcador_local,
            p.marcador_visitante,
            p.costo AS costo_base,
            p.fase::text AS fase,
            p.estado::text AS estado_partido,
            p.email_admin,
            p.fecha_habilitacion,
            est.id_estadio,
            est.nombre_estadio,
            est.capacidad AS capacidad_estadio,
            est.ubicacion,
            est.ciudad,
            est.pais::text AS pais_estadio,
            ps.nombre_sector::text AS nombre_sector,
            COALESCE(s.capacidad, 0) AS capacidad_sector,
            COALESCE(s.costo, 0) AS costo_sector,
            COUNT(e.id_entrada) FILTER (WHERE e.estado <> 'cancelada')::int AS entradas_vendidas
        FROM partido p
        INNER JOIN estadio est ON est.id_estadio = p.id_estadio
        LEFT JOIN partido_sector ps ON ps.id_partido = p.id_partido AND ps.id_estadio = p.id_estadio
        LEFT JOIN sector s ON s.nombre_sector = ps.nombre_sector AND s.id_estadio = ps.id_estadio
        LEFT JOIN entrada e ON e.id_partido = p.id_partido
            AND e.id_estadio = ps.id_estadio
            AND e.nombre_sector = ps.nombre_sector
        """;

    private const string GroupBySql = """
        GROUP BY
            p.id_partido,
            p.fecha,
            p.hora,
            p.equipo_local,
            p.equipo_visitante,
            p.marcador_local,
            p.marcador_visitante,
            p.costo,
            p.fase,
            p.estado,
            p.email_admin,
            p.fecha_habilitacion,
            est.id_estadio,
            est.nombre_estadio,
            est.capacidad,
            est.ubicacion,
            est.ciudad,
            est.pais,
            ps.nombre_sector,
            s.capacidad,
            s.costo
        """;

    private static async Task<List<EventoResponse>> MapEventosAsync(
        NpgsqlDataReader reader,
        CancellationToken cancellationToken)
    {
        var eventos = new Dictionary<int, EventoResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var idPartido = reader.GetInt32(reader.GetOrdinal("id_partido"));

            if (!eventos.TryGetValue(idPartido, out var evento))
            {
                evento = new EventoResponse
                {
                    IdPartido = idPartido,
                    Fecha = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha")),
                    Hora = reader.GetFieldValue<TimeOnly>(reader.GetOrdinal("hora")),
                    EquipoLocal = reader.GetString(reader.GetOrdinal("equipo_local")),
                    EquipoVisitante = reader.GetString(reader.GetOrdinal("equipo_visitante")),
                    MarcadorLocal = reader.GetInt32(reader.GetOrdinal("marcador_local")),
                    MarcadorVisitante = reader.GetInt32(reader.GetOrdinal("marcador_visitante")),
                    CostoBase = reader.GetInt32(reader.GetOrdinal("costo_base")),
                    Fase = reader.GetString(reader.GetOrdinal("fase")),
                    Estado = reader.GetString(reader.GetOrdinal("estado_partido")),
                    EmailAdmin = reader.GetString(reader.GetOrdinal("email_admin")),
                    FechaHabilitacion = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha_habilitacion")),
                    Estadio = new EstadioEventoResponse
                    {
                        IdEstadio = reader.GetInt32(reader.GetOrdinal("id_estadio")),
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
                        Pais = reader.GetString(reader.GetOrdinal("pais_estadio"))
                    },
                    Sectores = []
                };

                eventos.Add(idPartido, evento);
            }

            if (reader.IsDBNull(reader.GetOrdinal("nombre_sector")))
                continue;

            var capacidad = reader.GetInt32(reader.GetOrdinal("capacidad_sector"));
            var entradasVendidas = reader.GetInt32(reader.GetOrdinal("entradas_vendidas"));
            var costoSector = reader.GetInt32(reader.GetOrdinal("costo_sector"));

            evento.Sectores.Add(new SectorEventoResponse
            {
                NombreSector = reader.GetString(reader.GetOrdinal("nombre_sector")),
                Capacidad = capacidad,
                CostoSector = costoSector,
                CostoTotalEntrada = evento.CostoBase + costoSector,
                EntradasVendidas = entradasVendidas,
                EntradasDisponibles = Math.Max(0, capacidad - entradasVendidas)
            });
        }

        return eventos.Values.ToList();
    }
}
