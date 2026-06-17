using Backend_BDII.Common.Database;
using Backend_BDII.Modules.Home.DTOs;
using Npgsql;

namespace Backend_BDII.Modules.Home.Repositories;

public sealed class HomeRepository : IHomeRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public HomeRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<GeneralHomeResponse> GetGeneralAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string metricsSql = """
            SELECT
                (SELECT COUNT(*)::int FROM compra WHERE LOWER(email_usuario) = LOWER(@email) AND estado = 'paga') AS compras_pagas,
                (SELECT COUNT(*)::int FROM entrada e INNER JOIN compra c ON c.id_compra = e.id_compra WHERE LOWER(e.email_propietario_actual) = LOWER(@email) AND e.estado = 'activa' AND c.estado = 'paga') AS entradas_activas,
                (SELECT COUNT(*)::int FROM transferencia WHERE LOWER(email_destino) = LOWER(@email) AND estado = 'pendiente') AS transferencias_pendientes_recibidas,
                (SELECT COUNT(*)::int FROM transferencia WHERE LOWER(email_origen) = LOWER(@email) AND estado = 'pendiente') AS transferencias_pendientes_enviadas;
            """;

        int comprasPagas;
        int entradasActivas;
        int transferenciasRecibidas;
        int transferenciasEnviadas;

        await using (var command = new NpgsqlCommand(metricsSql, connection))
        {
            command.Parameters.AddWithValue("email", email);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);

            comprasPagas = reader.GetInt32(reader.GetOrdinal("compras_pagas"));
            entradasActivas = reader.GetInt32(reader.GetOrdinal("entradas_activas"));
            transferenciasRecibidas = reader.GetInt32(reader.GetOrdinal("transferencias_pendientes_recibidas"));
            transferenciasEnviadas = reader.GetInt32(reader.GetOrdinal("transferencias_pendientes_enviadas"));
        }

        var proximasEntradas = await GetProximasEntradasAsync(connection, email, cancellationToken);
        var transferenciasPendientes = await GetTransferenciasPendientesAsync(connection, email, cancellationToken);

        return new GeneralHomeResponse
        {
            ComprasPagas = comprasPagas,
            EntradasActivas = entradasActivas,
            TransferenciasPendientesRecibidas = transferenciasRecibidas,
            TransferenciasPendientesEnviadas = transferenciasEnviadas,
            ProximasEntradas = proximasEntradas,
            TransferenciasPendientes = transferenciasPendientes
        };
    }

    public async Task<AdminHomeResponse> GetAdminAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string metricsSql = """
            WITH pais_admin AS (
                SELECT pais
                FROM admin
                WHERE LOWER(email_admin) = LOWER(@email)
            ),
            partidos_admin AS (
                SELECT p.id_partido, p.fecha, p.hora
                FROM pais_admin pa
                INNER JOIN estadio est ON est.pais = pa.pais
                INNER JOIN partido p ON p.id_estadio = est.id_estadio
            ),
            entradas_pagas AS (
                SELECT e.id_entrada, e.costo_total
                FROM pais_admin pa
                INNER JOIN estadio est ON est.pais = pa.pais
                INNER JOIN partido p ON p.id_estadio = est.id_estadio
                INNER JOIN entrada e ON e.id_partido = p.id_partido
                INNER JOIN compra c ON c.id_compra = e.id_compra
                WHERE c.estado = 'paga'
            ),
            validaciones_admin AS (
                SELECT v.id_validacion, v.estado, v.fecha_hora
                FROM pais_admin pa
                INNER JOIN estadio est ON est.pais = pa.pais
                INNER JOIN partido p ON p.id_estadio = est.id_estadio
                INNER JOIN entrada e ON e.id_partido = p.id_partido
                INNER JOIN valida v ON v.id_entrada = e.id_entrada
            )
            SELECT
                (SELECT COUNT(*)::int FROM partidos_admin) AS eventos_totales,
                (
                    SELECT COUNT(*)::int
                    FROM partidos_admin
                    WHERE fecha > CURRENT_DATE OR (fecha = CURRENT_DATE AND hora > LOCALTIME)
                ) AS eventos_futuros,
                (SELECT COUNT(*)::int FROM entradas_pagas) AS entradas_vendidas,
                (SELECT COALESCE(SUM(costo_total), 0)::int FROM entradas_pagas) AS monto_vendido,
                (
                    SELECT COUNT(*)::int
                    FROM validaciones_admin
                    WHERE fecha_hora::date = CURRENT_DATE
                ) AS validaciones_hoy,
                (
                    SELECT COUNT(*)::int
                    FROM validaciones_admin
                    WHERE fecha_hora::date = CURRENT_DATE
                      AND estado = CAST(@estado_invalida AS estado_validacion_enum)
                ) AS validaciones_invalidas_hoy;
            """;

        int eventosTotales;
        int eventosFuturos;
        int entradasVendidas;
        int montoVendido;
        int validacionesHoy;
        int validacionesInvalidasHoy;

        await using (var command = new NpgsqlCommand(metricsSql, connection))
        {
            command.Parameters.AddWithValue("email", email);
            command.Parameters.AddWithValue("estado_invalida", "inv\u00e1lida");
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);

            eventosTotales = reader.GetInt32(reader.GetOrdinal("eventos_totales"));
            eventosFuturos = reader.GetInt32(reader.GetOrdinal("eventos_futuros"));
            entradasVendidas = reader.GetInt32(reader.GetOrdinal("entradas_vendidas"));
            montoVendido = reader.GetInt32(reader.GetOrdinal("monto_vendido"));
            validacionesHoy = reader.GetInt32(reader.GetOrdinal("validaciones_hoy"));
            validacionesInvalidasHoy = reader.GetInt32(reader.GetOrdinal("validaciones_invalidas_hoy"));
        }

        var proximosEventos = await GetProximosEventosAdminAsync(connection, email, cancellationToken);

        return new AdminHomeResponse
        {
            EventosTotales = eventosTotales,
            EventosFuturos = eventosFuturos,
            EntradasVendidas = entradasVendidas,
            MontoVendido = montoVendido,
            ValidacionesHoy = validacionesHoy,
            ValidacionesInvalidasHoy = validacionesInvalidasHoy,
            ProximosEventos = proximosEventos
        };
    }

    public async Task<FuncionarioHomeResponse> GetFuncionarioAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string metricsSql = """
            SELECT
                COUNT(DISTINCT d.id_dispositivo_escaneo)::int AS dispositivos_asignados,
                COUNT(DISTINCT d.id_dispositivo_escaneo) FILTER (WHERE d.activo = TRUE)::int AS dispositivos_activos,
                COUNT(v.id_validacion) FILTER (WHERE v.fecha_hora::date = CURRENT_DATE)::int AS validaciones_hoy,
                COUNT(v.id_validacion) FILTER (WHERE v.fecha_hora::date = CURRENT_DATE AND v.estado = CAST(@estado_valida AS estado_validacion_enum))::int AS validaciones_validas_hoy,
                COUNT(v.id_validacion) FILTER (WHERE v.fecha_hora::date = CURRENT_DATE AND v.estado = CAST(@estado_invalida AS estado_validacion_enum))::int AS validaciones_invalidas_hoy
            FROM dispositivo_escaneo d
            LEFT JOIN valida v ON v.id_dispositivo = d.id_dispositivo_escaneo
            WHERE LOWER(d.email_funcionario) = LOWER(@email);
            """;

        int dispositivosAsignados;
        int dispositivosActivos;
        int validacionesHoy;
        int validacionesValidasHoy;
        int validacionesInvalidasHoy;

        await using (var command = new NpgsqlCommand(metricsSql, connection))
        {
            command.Parameters.AddWithValue("email", email);
            command.Parameters.AddWithValue("estado_valida", "v\u00e1lida");
            command.Parameters.AddWithValue("estado_invalida", "inv\u00e1lida");
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);

            dispositivosAsignados = reader.GetInt32(reader.GetOrdinal("dispositivos_asignados"));
            dispositivosActivos = reader.GetInt32(reader.GetOrdinal("dispositivos_activos"));
            validacionesHoy = reader.GetInt32(reader.GetOrdinal("validaciones_hoy"));
            validacionesValidasHoy = reader.GetInt32(reader.GetOrdinal("validaciones_validas_hoy"));
            validacionesInvalidasHoy = reader.GetInt32(reader.GetOrdinal("validaciones_invalidas_hoy"));
        }

        var ultimasValidaciones = await GetUltimasValidacionesFuncionarioAsync(connection, email, cancellationToken);

        return new FuncionarioHomeResponse
        {
            DispositivosAsignados = dispositivosAsignados,
            DispositivosActivos = dispositivosActivos,
            ValidacionesHoy = validacionesHoy,
            ValidacionesValidasHoy = validacionesValidasHoy,
            ValidacionesInvalidasHoy = validacionesInvalidasHoy,
            UltimasValidaciones = ultimasValidaciones
        };
    }

    private static async Task<List<GeneralHomeEntradaResponse>> GetProximasEntradasAsync(
        NpgsqlConnection connection,
        string email,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                e.id_entrada,
                e.estado::text AS estado_entrada,
                e.nombre_sector::text AS nombre_sector,
                p.id_partido,
                p.fecha,
                p.hora,
                p.equipo_local,
                p.equipo_visitante,
                est.nombre_estadio,
                est.ciudad
            FROM entrada e
            INNER JOIN compra c ON c.id_compra = e.id_compra
            INNER JOIN partido p ON p.id_partido = e.id_partido
            INNER JOIN estadio est ON est.id_estadio = e.id_estadio
            WHERE LOWER(e.email_propietario_actual) = LOWER(@email)
              AND e.estado = 'activa'
              AND c.estado = 'paga'
              AND (p.fecha > CURRENT_DATE OR (p.fecha = CURRENT_DATE AND p.hora > LOCALTIME))
            ORDER BY p.fecha, p.hora, e.id_entrada
            LIMIT 5;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email", email);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var entradas = new List<GeneralHomeEntradaResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            entradas.Add(new GeneralHomeEntradaResponse
            {
                IdEntrada = reader.GetInt32(reader.GetOrdinal("id_entrada")),
                Estado = reader.GetString(reader.GetOrdinal("estado_entrada")),
                NombreSector = reader.GetString(reader.GetOrdinal("nombre_sector")),
                IdPartido = reader.GetInt32(reader.GetOrdinal("id_partido")),
                FechaPartido = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha")),
                HoraPartido = reader.GetFieldValue<TimeOnly>(reader.GetOrdinal("hora")),
                EquipoLocal = reader.GetString(reader.GetOrdinal("equipo_local")),
                EquipoVisitante = reader.GetString(reader.GetOrdinal("equipo_visitante")),
                Estadio = reader.GetString(reader.GetOrdinal("nombre_estadio")),
                Ciudad = reader.IsDBNull(reader.GetOrdinal("ciudad")) ? null : reader.GetString(reader.GetOrdinal("ciudad"))
            });
        }

        return entradas;
    }

    private static async Task<List<GeneralHomeTransferenciaResponse>> GetTransferenciasPendientesAsync(
        NpgsqlConnection connection,
        string email,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id_transferencia, id_entrada, email_origen, email_destino, fecha_hora
            FROM transferencia
            WHERE estado = 'pendiente'
              AND (LOWER(email_origen) = LOWER(@email) OR LOWER(email_destino) = LOWER(@email))
            ORDER BY fecha_hora DESC, id_transferencia DESC
            LIMIT 5;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email", email);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var transferencias = new List<GeneralHomeTransferenciaResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            transferencias.Add(new GeneralHomeTransferenciaResponse
            {
                IdTransferencia = reader.GetInt32(reader.GetOrdinal("id_transferencia")),
                IdEntrada = reader.GetInt32(reader.GetOrdinal("id_entrada")),
                EmailOrigen = reader.GetString(reader.GetOrdinal("email_origen")),
                EmailDestino = reader.GetString(reader.GetOrdinal("email_destino")),
                FechaHora = reader.GetDateTime(reader.GetOrdinal("fecha_hora"))
            });
        }

        return transferencias;
    }

    private static async Task<List<AdminHomeEventoResponse>> GetProximosEventosAdminAsync(
        NpgsqlConnection connection,
        string email,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                p.id_partido,
                p.fecha,
                p.hora,
                p.equipo_local,
                p.equipo_visitante,
                est.nombre_estadio,
                COUNT(e.id_entrada) FILTER (WHERE c.estado = 'paga')::int AS entradas_vendidas
            FROM admin a
            INNER JOIN estadio est ON est.pais = a.pais
            INNER JOIN partido p ON p.id_estadio = est.id_estadio
            LEFT JOIN entrada e ON e.id_partido = p.id_partido
            LEFT JOIN compra c ON c.id_compra = e.id_compra
            WHERE LOWER(a.email_admin) = LOWER(@email)
              AND (p.fecha > CURRENT_DATE OR (p.fecha = CURRENT_DATE AND p.hora > LOCALTIME))
            GROUP BY p.id_partido, p.fecha, p.hora, p.equipo_local, p.equipo_visitante, est.nombre_estadio
            ORDER BY p.fecha, p.hora
            LIMIT 5;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email", email);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var eventos = new List<AdminHomeEventoResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            eventos.Add(new AdminHomeEventoResponse
            {
                IdPartido = reader.GetInt32(reader.GetOrdinal("id_partido")),
                Fecha = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha")),
                Hora = reader.GetFieldValue<TimeOnly>(reader.GetOrdinal("hora")),
                EquipoLocal = reader.GetString(reader.GetOrdinal("equipo_local")),
                EquipoVisitante = reader.GetString(reader.GetOrdinal("equipo_visitante")),
                Estadio = reader.GetString(reader.GetOrdinal("nombre_estadio")),
                EntradasVendidas = reader.GetInt32(reader.GetOrdinal("entradas_vendidas"))
            });
        }

        return eventos;
    }

    private static async Task<List<FuncionarioHomeValidacionResponse>> GetUltimasValidacionesFuncionarioAsync(
        NpgsqlConnection connection,
        string email,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                v.id_validacion,
                v.id_entrada,
                v.id_dispositivo,
                v.estado::text AS estado_validacion,
                v.fecha_hora,
                p.equipo_local,
                p.equipo_visitante
            FROM valida v
            INNER JOIN dispositivo_escaneo d ON d.id_dispositivo_escaneo = v.id_dispositivo
            INNER JOIN entrada e ON e.id_entrada = v.id_entrada
            INNER JOIN partido p ON p.id_partido = e.id_partido
            WHERE LOWER(d.email_funcionario) = LOWER(@email)
            ORDER BY v.fecha_hora DESC, v.id_validacion DESC
            LIMIT 10;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email", email);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var validaciones = new List<FuncionarioHomeValidacionResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            validaciones.Add(new FuncionarioHomeValidacionResponse
            {
                IdValidacion = reader.GetInt32(reader.GetOrdinal("id_validacion")),
                IdEntrada = reader.GetInt32(reader.GetOrdinal("id_entrada")),
                IdDispositivo = reader.GetInt32(reader.GetOrdinal("id_dispositivo")),
                Estado = reader.GetString(reader.GetOrdinal("estado_validacion")),
                FechaHora = reader.GetDateTime(reader.GetOrdinal("fecha_hora")),
                EquipoLocal = reader.GetString(reader.GetOrdinal("equipo_local")),
                EquipoVisitante = reader.GetString(reader.GetOrdinal("equipo_visitante"))
            });
        }

        return validaciones;
    }
}
