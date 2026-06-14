using System.Text;
using Backend_BDII.Common.Database;
using Backend_BDII.Common.Domain;
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

    public async Task<List<EventoMasVendidoResponse>> GetEventosMasVendidosAsync(
        string? pais,
        int limit,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var sql = new StringBuilder("""
            SELECT
                p.id_partido,
                p.fecha,
                p.hora,
                p.equipo_local,
                p.equipo_visitante,
                est.nombre_estadio,
                est.pais::text AS pais_estadio,
                COUNT(e.id_entrada)::int AS entradas_vendidas,
                COALESCE(SUM(e.costo_total), 0)::int AS monto_vendido
            FROM partido p
            INNER JOIN estadio est ON est.id_estadio = p.id_estadio
            INNER JOIN entrada e ON e.id_partido = p.id_partido
            INNER JOIN compra c ON c.id_compra = e.id_compra
            WHERE c.estado = 'paga'
              AND e.estado <> 'cancelada'
            """);
        sql.AppendLine();

        if (!string.IsNullOrWhiteSpace(pais))
            sql.AppendLine("AND est.pais::text = @pais");

        sql.AppendLine("""
            GROUP BY
                p.id_partido,
                p.fecha,
                p.hora,
                p.equipo_local,
                p.equipo_visitante,
                est.nombre_estadio,
                est.pais
            ORDER BY entradas_vendidas DESC, monto_vendido DESC
            LIMIT @limit;
            """);

        await using var command = new NpgsqlCommand(sql.ToString(), connection);
        command.Parameters.AddWithValue("limit", limit);

        if (!string.IsNullOrWhiteSpace(pais))
            command.Parameters.AddWithValue("pais", PaisSedeNormalizer.Normalize(pais));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var eventos = new List<EventoMasVendidoResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            eventos.Add(new EventoMasVendidoResponse
            {
                IdPartido = reader.GetInt32(reader.GetOrdinal("id_partido")),
                Fecha = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha")),
                Hora = reader.GetFieldValue<TimeOnly>(reader.GetOrdinal("hora")),
                EquipoLocal = reader.GetString(reader.GetOrdinal("equipo_local")),
                EquipoVisitante = reader.GetString(reader.GetOrdinal("equipo_visitante")),
                Estadio = reader.GetString(reader.GetOrdinal("nombre_estadio")),
                Pais = reader.GetString(reader.GetOrdinal("pais_estadio")),
                EntradasVendidas = reader.GetInt32(reader.GetOrdinal("entradas_vendidas")),
                MontoVendido = reader.GetInt32(reader.GetOrdinal("monto_vendido"))
            });
        }

        return eventos;
    }

    public async Task<List<MayorCompradorResponse>> GetMayoresCompradoresAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                u.email,
                u.nombre,
                COUNT(c.id_compra)::int AS compras_pagas,
                COALESCE(SUM(ent.cantidad), 0)::int AS entradas_compradas,
                COALESCE(SUM(c.monto_total), 0)::int AS monto_total_pagado
            FROM usuario u
            INNER JOIN compra c ON c.email_usuario = u.email AND c.estado = 'paga'
            LEFT JOIN (
                SELECT id_compra, COUNT(*)::int AS cantidad
                FROM entrada
                WHERE estado <> 'cancelada'
                GROUP BY id_compra
            ) ent ON ent.id_compra = c.id_compra
            GROUP BY u.email, u.nombre
            ORDER BY monto_total_pagado DESC, entradas_compradas DESC
            LIMIT @limit;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("limit", limit);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var compradores = new List<MayorCompradorResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            compradores.Add(new MayorCompradorResponse
            {
                EmailUsuario = reader.GetString(reader.GetOrdinal("email")),
                NombreUsuario = reader.GetString(reader.GetOrdinal("nombre")),
                ComprasPagas = reader.GetInt32(reader.GetOrdinal("compras_pagas")),
                EntradasCompradas = reader.GetInt32(reader.GetOrdinal("entradas_compradas")),
                MontoTotalPagado = reader.GetInt32(reader.GetOrdinal("monto_total_pagado"))
            });
        }

        return compradores;
    }

    public async Task<List<OcupacionEventoResponse>> GetOcupacionEventosAsync(
        string? pais,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var sql = new StringBuilder("""
            SELECT
                p.id_partido,
                p.fecha,
                p.hora,
                p.equipo_local,
                p.equipo_visitante,
                est.id_estadio,
                est.nombre_estadio,
                est.ciudad,
                est.pais::text AS pais_estadio,
                COALESCE(cap.capacidad_habilitada, 0)::int AS capacidad_habilitada,
                COALESCE(vend.entradas_vendidas, 0)::int AS entradas_vendidas
            FROM partido p
            INNER JOIN estadio est ON est.id_estadio = p.id_estadio
            LEFT JOIN (
                SELECT ps.id_partido, SUM(s.capacidad)::int AS capacidad_habilitada
                FROM partido_sector ps
                INNER JOIN sector s ON s.nombre_sector = ps.nombre_sector AND s.id_estadio = ps.id_estadio
                GROUP BY ps.id_partido
            ) cap ON cap.id_partido = p.id_partido
            LEFT JOIN (
                SELECT id_partido, COUNT(*)::int AS entradas_vendidas
                FROM entrada
                WHERE estado <> 'cancelada'
                GROUP BY id_partido
            ) vend ON vend.id_partido = p.id_partido
            """);
        sql.AppendLine();

        if (!string.IsNullOrWhiteSpace(pais))
            sql.AppendLine("WHERE est.pais::text = @pais");

        sql.AppendLine("""
            ORDER BY
                CASE WHEN COALESCE(cap.capacidad_habilitada, 0) = 0 THEN 0
                     ELSE COALESCE(vend.entradas_vendidas, 0)::numeric / cap.capacidad_habilitada
                END DESC,
                p.fecha,
                p.hora;
            """);

        await using var command = new NpgsqlCommand(sql.ToString(), connection);

        if (!string.IsNullOrWhiteSpace(pais))
            command.Parameters.AddWithValue("pais", PaisSedeNormalizer.Normalize(pais));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var ocupaciones = new List<OcupacionEventoResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var capacidad = reader.GetInt32(reader.GetOrdinal("capacidad_habilitada"));
            var vendidas = reader.GetInt32(reader.GetOrdinal("entradas_vendidas"));

            ocupaciones.Add(new OcupacionEventoResponse
            {
                IdPartido = reader.GetInt32(reader.GetOrdinal("id_partido")),
                Fecha = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha")),
                Hora = reader.GetFieldValue<TimeOnly>(reader.GetOrdinal("hora")),
                EquipoLocal = reader.GetString(reader.GetOrdinal("equipo_local")),
                EquipoVisitante = reader.GetString(reader.GetOrdinal("equipo_visitante")),
                IdEstadio = reader.GetInt32(reader.GetOrdinal("id_estadio")),
                Estadio = reader.GetString(reader.GetOrdinal("nombre_estadio")),
                Ciudad = reader.IsDBNull(reader.GetOrdinal("ciudad"))
                    ? ""
                    : reader.GetString(reader.GetOrdinal("ciudad")),
                Pais = reader.GetString(reader.GetOrdinal("pais_estadio")),
                CapacidadHabilitada = capacidad,
                EntradasVendidas = vendidas,
                PorcentajeOcupacion = capacidad == 0
                    ? 0
                    : Math.Round(vendidas * 100.0 / capacidad, 2)
            });
        }

        return ocupaciones;
    }

    public async Task<ResumenValidacionesResponse> GetResumenValidacionesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                COUNT(*)::int AS total_validaciones,
                COUNT(*) FILTER (WHERE estado = 'válida')::int AS validaciones_validas,
                COUNT(*) FILTER (WHERE estado = 'inválida')::int AS validaciones_invalidas,
                COUNT(DISTINCT id_entrada) FILTER (WHERE estado = 'válida')::int AS entradas_consumidas
            FROM valida;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
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
