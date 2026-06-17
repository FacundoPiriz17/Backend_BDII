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

    public async Task<List<EventoResponse>> GetAllAsync(
        bool soloFuturos,
        string? busqueda,
        string? pais,
        string? equipo,
        string? fase,
        string? estado,
        DateOnly? desde,
        DateOnly? hasta,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var sql = new StringBuilder(BaseSelectSql);
        sql.AppendLine();
        sql.AppendLine("WHERE 1 = 1");

        if (soloFuturos)
        {
            sql.AppendLine("""
                AND (
                    p.fecha > CURRENT_DATE
                    OR (p.fecha = CURRENT_DATE AND p.hora > LOCALTIME)
                )
                """);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            sql.AppendLine("""
                AND (
                    p.equipo_local ILIKE @busqueda
                    OR p.equipo_visitante ILIKE @busqueda
                    OR est.nombre_estadio ILIKE @busqueda
                    OR est.ciudad ILIKE @busqueda
                )
                """);
        }

        if (!string.IsNullOrWhiteSpace(pais))
            sql.AppendLine("AND est.pais::text = @pais");

        if (!string.IsNullOrWhiteSpace(equipo))
            sql.AppendLine("AND (p.equipo_local = @equipo OR p.equipo_visitante = @equipo)");

        if (!string.IsNullOrWhiteSpace(fase))
            sql.AppendLine("AND p.fase = CAST(@fase AS fase_enum)");

        if (!string.IsNullOrWhiteSpace(estado))
            sql.AppendLine("AND p.estado = CAST(@estado AS estado_partido_enum)");

        if (desde.HasValue)
            sql.AppendLine("AND p.fecha >= @desde");

        if (hasta.HasValue)
            sql.AppendLine("AND p.fecha <= @hasta");

        sql.AppendLine(BaseGroupBySql);
        sql.AppendLine("ORDER BY p.fecha, p.hora, p.id_partido, ps.nombre_sector;");

        await using var command = new NpgsqlCommand(sql.ToString(), connection);

        if (!string.IsNullOrWhiteSpace(busqueda))
            command.Parameters.AddWithValue("busqueda", $"%{busqueda.Trim()}%");

        if (!string.IsNullOrWhiteSpace(pais))
            command.Parameters.AddWithValue("pais", PaisSedeNormalizer.Normalize(pais));

        if (!string.IsNullOrWhiteSpace(equipo))
            command.Parameters.AddWithValue("equipo", NormalizeEquipo(equipo));

        if (!string.IsNullOrWhiteSpace(fase))
            command.Parameters.AddWithValue("fase", fase.Trim());

        if (!string.IsNullOrWhiteSpace(estado))
            command.Parameters.AddWithValue("estado", estado.Trim().ToLowerInvariant());

        if (desde.HasValue)
            command.Parameters.AddWithValue("desde", desde.Value);

        if (hasta.HasValue)
            command.Parameters.AddWithValue("hasta", hasta.Value);

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
                    @fecha_habilitacion
                )
                RETURNING id_partido;
                """;

            int idPartido;

            await using (var command = new NpgsqlCommand(insertSql, connection, transaction))
            {
                command.Parameters.AddWithValue("fecha", request.Fecha);
                command.Parameters.AddWithValue("hora", request.Hora);
                command.Parameters.AddWithValue("id_estadio", request.IdEstadio);
                command.Parameters.AddWithValue("equipo_visitante", NormalizeEquipo(request.EquipoVisitante));
                command.Parameters.AddWithValue("equipo_local", NormalizeEquipo(request.EquipoLocal));
                command.Parameters.AddWithValue("costo", request.Costo);
                command.Parameters.AddWithValue("fase", request.Fase.Trim());
                command.Parameters.AddWithValue("email_admin", emailAdmin);
                command.Parameters.AddWithValue(
                    "fecha_habilitacion",
                    request.FechaHabilitacion ?? DateOnly.FromDateTime(DateTime.Today));

                idPartido = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            }

            await ReemplazarSectoresAsync(connection, transaction, idPartido, request.SectoresHabilitados, cancellationToken);

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
            const string updateSql = """
                UPDATE partido
                SET
                    fecha = @fecha,
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
                WHERE id_partido = @id_partido;
                """;

            int affectedRows;

            await using (var command = new NpgsqlCommand(updateSql, connection, transaction))
            {
                command.Parameters.AddWithValue("id_partido", idPartido);
                command.Parameters.AddWithValue("fecha", request.Fecha);
                command.Parameters.AddWithValue("hora", request.Hora);
                command.Parameters.AddWithValue("id_estadio", request.IdEstadio);
                command.Parameters.AddWithValue("equipo_visitante", NormalizeEquipo(request.EquipoVisitante));
                command.Parameters.AddWithValue("equipo_local", NormalizeEquipo(request.EquipoLocal));
                command.Parameters.AddWithValue("marcador_local", request.MarcadorLocal);
                command.Parameters.AddWithValue("marcador_visitante", request.MarcadorVisitante);
                command.Parameters.AddWithValue("costo", request.Costo);
                command.Parameters.AddWithValue("fase", request.Fase.Trim());
                command.Parameters.AddWithValue("estado", request.Estado.Trim().ToLowerInvariant());
                command.Parameters.AddWithValue("email_admin", emailAdmin);
                command.Parameters.AddWithValue("fecha_habilitacion", request.FechaHabilitacion);

                affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
            }

            if (affectedRows == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            await ReemplazarSectoresAsync(connection, transaction, idPartido, request.SectoresHabilitados, cancellationToken);

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
        string estado,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            UPDATE partido
            SET estado = CAST(@estado AS estado_partido_enum),
                email_admin = @email_admin
            WHERE id_partido = @id_partido;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id_partido", idPartido);
        command.Parameters.AddWithValue("email_admin", emailAdmin);
        command.Parameters.AddWithValue("estado", estado);

        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);

        if (affectedRows == 0)
            return null;

        return await GetByIdUsingConnectionAsync(connection, idPartido, null, cancellationToken);
    }

    private static async Task ReemplazarSectoresAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int idPartido,
        IEnumerable<string> sectores,
        CancellationToken cancellationToken)
    {
        const string deleteSql = """
            DELETE FROM partido_sector
            WHERE id_partido = @id_partido;
            """;

        await using (var command = new NpgsqlCommand(deleteSql, connection, transaction))
        {
            command.Parameters.AddWithValue("id_partido", idPartido);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        const string insertSql = """
            INSERT INTO partido_sector (id_partido, nombre_sector, id_estadio)
            SELECT p.id_partido, CAST(@nombre_sector AS sector_enum), p.id_estadio
            FROM partido p
            INNER JOIN sector s ON s.id_estadio = p.id_estadio
                AND s.nombre_sector = CAST(@nombre_sector AS sector_enum)
            WHERE p.id_partido = @id_partido;
            """;

        foreach (var sector in sectores.Select(s => s.Trim().ToUpperInvariant()).Distinct())
        {
            await using var command = new NpgsqlCommand(insertSql, connection, transaction);
            command.Parameters.AddWithValue("id_partido", idPartido);
            command.Parameters.AddWithValue("nombre_sector", sector);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task<EventoResponse?> GetByIdUsingConnectionAsync(
        NpgsqlConnection connection,
        int idPartido,
        NpgsqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        var sql = BaseSelectSql + "\n" + """
            WHERE p.id_partido = @id_partido
            """ + "\n" + BaseGroupBySql + "\n" + """
            ORDER BY ps.nombre_sector;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id_partido", idPartido);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var eventos = await MapEventosAsync(reader, cancellationToken);

        return eventos.FirstOrDefault();
    }

    private const string BaseSelectSql = """
        SELECT
            p.id_partido,
            p.fecha AS fecha_partido,
            p.hora AS hora_partido,
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
            COUNT(e.id_entrada)::int AS entradas_vendidas
        FROM partido p
        INNER JOIN estadio est ON est.id_estadio = p.id_estadio
        LEFT JOIN partido_sector ps ON ps.id_partido = p.id_partido AND ps.id_estadio = p.id_estadio
        LEFT JOIN sector s ON s.id_estadio = ps.id_estadio AND s.nombre_sector = ps.nombre_sector
        LEFT JOIN entrada e ON e.id_partido = p.id_partido
            AND e.id_estadio = ps.id_estadio
            AND e.nombre_sector = ps.nombre_sector
        """;

    private const string BaseGroupBySql = """
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
                    Fecha = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha_partido")),
                    Hora = reader.GetFieldValue<TimeOnly>(reader.GetOrdinal("hora_partido")),
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
            var vendidas = reader.GetInt32(reader.GetOrdinal("entradas_vendidas"));
            var costoSector = reader.GetInt32(reader.GetOrdinal("costo_sector"));

            evento.Sectores.Add(new SectorEventoResponse
            {
                NombreSector = reader.GetString(reader.GetOrdinal("nombre_sector")),
                Capacidad = capacidad,
                CostoSector = costoSector,
                CostoTotalEntrada = evento.CostoBase + costoSector,
                EntradasVendidas = vendidas,
                EntradasDisponibles = Math.Max(0, capacidad - vendidas)
            });
        }

        return eventos.Values.ToList();
    }

    private static string NormalizeEquipo(string codigoFifa)
    {
        return codigoFifa.Trim().ToUpperInvariant();
    }
}
