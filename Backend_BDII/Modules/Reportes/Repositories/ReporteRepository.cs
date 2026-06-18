using Backend_BDII.Common.Database;
using Backend_BDII.Modules.Reportes.DTOs;
using Npgsql;

namespace Backend_BDII.Modules.Reportes.Repositories;

public sealed class ReporteRepository : IReporteRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ReporteRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<AuditoriaEntradaResponse>> GetAuditoriaAsync(
        string? tipo,
        int limit,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT tipo, fecha, usuario, estado, monto, detalle, id_referencia
            FROM (
                SELECT 'compra' AS tipo, c.fecha_hora AS fecha, c.email_usuario AS usuario,
                       c.estado::text AS estado, c.monto_total AS monto,
                       NULL::text AS detalle, c.id_compra AS id_referencia
                FROM compra c
                UNION ALL
                SELECT 'transferencia', t.fecha_hora, t.email_origen,
                       t.estado::text, NULL::int,
                       (t.email_origen || ' → ' || t.email_destino), t.id_transferencia
                FROM transferencia t
                UNION ALL
                SELECT 'validacion', v.fecha_hora, d.email_funcionario,
                       v.estado::text, NULL::int,
                       ('Entrada #' || v.id_entrada::text), v.id_validacion
                FROM valida v
                INNER JOIN dispositivo_escaneo d ON d.id_dispositivo_escaneo = v.id_dispositivo
            ) auditoria
            WHERE (@tipo::text IS NULL OR tipo = @tipo::text)
            ORDER BY fecha DESC
            LIMIT @limit;
            """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.Add(new NpgsqlParameter("tipo", NpgsqlTypes.NpgsqlDbType.Text)
        {
            Value = (object?)tipo ?? DBNull.Value
        });
        command.Parameters.AddWithValue("limit", limit);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var items = new List<AuditoriaEntradaResponse>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new AuditoriaEntradaResponse
            {
                Tipo = reader.GetString(reader.GetOrdinal("tipo")),
                Fecha = reader.GetDateTime(reader.GetOrdinal("fecha")),
                Usuario = reader.IsDBNull(reader.GetOrdinal("usuario"))
                    ? "—"
                    : reader.GetString(reader.GetOrdinal("usuario")),
                Estado = reader.IsDBNull(reader.GetOrdinal("estado"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("estado")),
                Monto = reader.IsDBNull(reader.GetOrdinal("monto"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("monto")),
                Detalle = reader.IsDBNull(reader.GetOrdinal("detalle"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("detalle")),
                IdReferencia = reader.GetInt32(reader.GetOrdinal("id_referencia")),
            });
        }

        return items;
    }

    public async Task<List<EventoMasVendidoResponse>> GetEventosMasVendidosAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                p.id_partido,
                p.fecha AS fecha_partido,
                p.hora AS hora_partido,
                p.equipo_local,
                p.equipo_visitante,
                est.nombre_estadio,
                est.pais::text AS pais_estadio,
                (COUNT(e.id_entrada) FILTER (WHERE c.estado = 'paga'))::int AS entradas_vendidas,
                COALESCE(SUM(e.costo_total) FILTER (WHERE c.estado = 'paga'), 0)::int AS monto_vendido
            FROM partido p
            INNER JOIN estadio est ON est.id_estadio = p.id_estadio
            LEFT JOIN entrada e ON e.id_partido = p.id_partido
            LEFT JOIN compra c ON c.id_compra = e.id_compra
            GROUP BY
                p.id_partido,
                p.fecha,
                p.hora,
                p.equipo_local,
                p.equipo_visitante,
                est.nombre_estadio,
                est.pais
            ORDER BY entradas_vendidas DESC, monto_vendido DESC, p.fecha
            LIMIT @limit;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("limit", limit);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var reportes = new List<EventoMasVendidoResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            reportes.Add(new EventoMasVendidoResponse
            {
                IdPartido = reader.GetInt32(reader.GetOrdinal("id_partido")),
                Fecha = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha_partido")),
                Hora = reader.GetFieldValue<TimeOnly>(reader.GetOrdinal("hora_partido")),
                EquipoLocal = reader.GetString(reader.GetOrdinal("equipo_local")),
                EquipoVisitante = reader.GetString(reader.GetOrdinal("equipo_visitante")),
                Estadio = reader.GetString(reader.GetOrdinal("nombre_estadio")),
                Pais = reader.GetString(reader.GetOrdinal("pais_estadio")),
                EntradasVendidas = reader.GetInt32(reader.GetOrdinal("entradas_vendidas")),
                MontoVendido = reader.GetInt32(reader.GetOrdinal("monto_vendido"))
            });
        }

        return reportes;
    }

    public async Task<List<MayorCompradorResponse>> GetMayoresCompradoresAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                c.email_usuario,
                u.nombre,
                COUNT(c.id_compra)::int AS compras_pagas,
                COALESCE(SUM(ec.entradas), 0)::int AS entradas_compradas,
                COALESCE(SUM(c.monto_total), 0)::int AS monto_total_pagado
            FROM compra c
            INNER JOIN usuario u ON u.email = c.email_usuario
            LEFT JOIN (
                SELECT id_compra, COUNT(*)::int AS entradas
                FROM entrada
                GROUP BY id_compra
            ) ec ON ec.id_compra = c.id_compra
            WHERE c.estado = 'paga'
            GROUP BY c.email_usuario, u.nombre
            ORDER BY entradas_compradas DESC, monto_total_pagado DESC, c.email_usuario
            LIMIT @limit;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("limit", limit);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var reportes = new List<MayorCompradorResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            reportes.Add(new MayorCompradorResponse
            {
                EmailUsuario = reader.GetString(reader.GetOrdinal("email_usuario")),
                NombreUsuario = reader.GetString(reader.GetOrdinal("nombre")),
                ComprasPagas = reader.GetInt32(reader.GetOrdinal("compras_pagas")),
                EntradasCompradas = reader.GetInt32(reader.GetOrdinal("entradas_compradas")),
                MontoTotalPagado = reader.GetInt32(reader.GetOrdinal("monto_total_pagado"))
            });
        }

        return reportes;
    }

    public async Task<List<OcupacionEventoResponse>> GetOcupacionEventosAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            WITH capacidad_evento AS (
                SELECT
                    ps.id_partido,
                    SUM(COALESCE(s.capacidad, 0))::int AS capacidad_habilitada
                FROM partido_sector ps
                INNER JOIN sector s ON s.id_estadio = ps.id_estadio
                    AND s.nombre_sector = ps.nombre_sector
                GROUP BY ps.id_partido
            ),
            ventas_evento AS (
                SELECT
                    e.id_partido,
                    COUNT(*)::int AS entradas_vendidas
                FROM entrada e
                INNER JOIN compra c ON c.id_compra = e.id_compra
                WHERE c.estado = 'paga'
                GROUP BY e.id_partido
            )
            SELECT
                p.id_partido,
                p.fecha AS fecha_partido,
                p.hora AS hora_partido,
                p.equipo_local,
                p.equipo_visitante,
                est.id_estadio,
                est.nombre_estadio,
                COALESCE(est.ciudad, '') AS ciudad,
                est.pais::text AS pais_estadio,
                COALESCE(cap.capacidad_habilitada, 0) AS capacidad_habilitada,
                COALESCE(ven.entradas_vendidas, 0) AS entradas_vendidas
            FROM partido p
            INNER JOIN estadio est ON est.id_estadio = p.id_estadio
            LEFT JOIN capacidad_evento cap ON cap.id_partido = p.id_partido
            LEFT JOIN ventas_evento ven ON ven.id_partido = p.id_partido
            ORDER BY p.fecha, p.hora
            LIMIT @limit;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("limit", limit);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var reportes = new List<OcupacionEventoResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var capacidad = reader.GetInt32(reader.GetOrdinal("capacidad_habilitada"));
            var vendidas = reader.GetInt32(reader.GetOrdinal("entradas_vendidas"));

            reportes.Add(new OcupacionEventoResponse
            {
                IdPartido = reader.GetInt32(reader.GetOrdinal("id_partido")),
                Fecha = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha_partido")),
                Hora = reader.GetFieldValue<TimeOnly>(reader.GetOrdinal("hora_partido")),
                EquipoLocal = reader.GetString(reader.GetOrdinal("equipo_local")),
                EquipoVisitante = reader.GetString(reader.GetOrdinal("equipo_visitante")),
                IdEstadio = reader.GetInt32(reader.GetOrdinal("id_estadio")),
                Estadio = reader.GetString(reader.GetOrdinal("nombre_estadio")),
                Ciudad = reader.GetString(reader.GetOrdinal("ciudad")),
                Pais = reader.GetString(reader.GetOrdinal("pais_estadio")),
                CapacidadHabilitada = capacidad,
                EntradasVendidas = vendidas,
                PorcentajeOcupacion = capacidad == 0 ? 0 : Math.Round(vendidas * 100.0 / capacidad, 2)
            });
        }

        return reportes;
    }

    public async Task<ResumenValidacionesResponse> GetResumenValidacionesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                (SELECT COUNT(*)::int FROM valida) AS total_validaciones,
                (SELECT COUNT(*)::int FROM valida WHERE estado::text = @estado_valida) AS validaciones_validas,
                (SELECT COUNT(*)::int FROM valida WHERE estado::text = @estado_invalida) AS validaciones_invalidas,
                (SELECT COUNT(*)::int FROM entrada WHERE estado = 'consumida') AS entradas_consumidas;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("estado_valida", "v\u00e1lida");
        command.Parameters.AddWithValue("estado_invalida", "inv\u00e1lida");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new ResumenValidacionesResponse
            {
                TotalValidaciones = 0,
                ValidacionesValidas = 0,
                ValidacionesInvalidas = 0,
                EntradasConsumidas = 0
            };
        }

        return new ResumenValidacionesResponse
        {
            TotalValidaciones = reader.GetInt32(reader.GetOrdinal("total_validaciones")),
            ValidacionesValidas = reader.GetInt32(reader.GetOrdinal("validaciones_validas")),
            ValidacionesInvalidas = reader.GetInt32(reader.GetOrdinal("validaciones_invalidas")),
            EntradasConsumidas = reader.GetInt32(reader.GetOrdinal("entradas_consumidas"))
        };
    }
}
