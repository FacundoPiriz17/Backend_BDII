using Backend_BDII.Common.Database;
using Backend_BDII.Modules.Entradas.DTOs;
using Npgsql;

namespace Backend_BDII.Modules.Entradas.Repositories;

public sealed class EntradaRepository : IEntradaRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EntradaRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<EntradaDetalleResponse?> GetByIdAsync(
        int idEntrada,
        string emailUsuario,
        bool puedeVerTodas,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var sql = BaseEntradaSql + "\n" + """
            WHERE e.id_entrada = @id_entrada
              AND (
                    @puede_ver_todas = TRUE
                    OR LOWER(e.email_propietario_actual) = LOWER(@email_usuario)
                  );
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id_entrada", idEntrada);
        command.Parameters.AddWithValue("email_usuario", emailUsuario);
        command.Parameters.AddWithValue("puede_ver_todas", puedeVerTodas);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return MapEntrada(reader);
    }

    public async Task<CustodiaEntradaResponse?> GetCustodiaAsync(
        int idEntrada,
        string emailUsuario,
        bool puedeVerTodas,
        CancellationToken cancellationToken = default)
    {
        var entrada = await GetByIdAsync(idEntrada, emailUsuario, puedeVerTodas, cancellationToken);

        if (entrada is null)
            return null;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                'emision' AS tipo,
                c.id_compra AS id_referencia,
                c.fecha_hora,
                c.estado::text AS estado,
                NULL::varchar AS email_origen,
                c.email_usuario AS email_destino,
                'Entrada emitida por compra' AS detalle
            FROM entrada e
            INNER JOIN compra c ON c.id_compra = e.id_compra
            WHERE e.id_entrada = @id_entrada

            UNION ALL

            SELECT
                'transferencia' AS tipo,
                t.id_transferencia AS id_referencia,
                t.fecha_hora,
                t.estado::text AS estado,
                t.email_origen,
                t.email_destino,
                'Transferencia de titularidad' AS detalle
            FROM transferencia t
            WHERE t.id_entrada = @id_entrada

            UNION ALL

            SELECT
                'validacion' AS tipo,
                v.id_validacion AS id_referencia,
                v.fecha_hora,
                v.estado::text AS estado,
                d.email_funcionario AS email_origen,
                e.email_propietario_actual AS email_destino,
                'Validacion en puerta' AS detalle
            FROM valida v
            INNER JOIN dispositivo_escaneo d ON d.id_dispositivo_escaneo = v.id_dispositivo
            INNER JOIN entrada e ON e.id_entrada = v.id_entrada
            WHERE v.id_entrada = @id_entrada

            ORDER BY fecha_hora, tipo;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id_entrada", idEntrada);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var eventos = new List<EventoCustodiaResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            eventos.Add(new EventoCustodiaResponse
            {
                Tipo = reader.GetString(reader.GetOrdinal("tipo")),
                IdReferencia = reader.GetInt32(reader.GetOrdinal("id_referencia")),
                FechaHora = reader.GetDateTime(reader.GetOrdinal("fecha_hora")),
                Estado = reader.GetString(reader.GetOrdinal("estado")),
                EmailOrigen = reader.IsDBNull(reader.GetOrdinal("email_origen"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("email_origen")),
                EmailDestino = reader.IsDBNull(reader.GetOrdinal("email_destino"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("email_destino")),
                Detalle = reader.IsDBNull(reader.GetOrdinal("detalle"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("detalle"))
            });
        }

        return new CustodiaEntradaResponse
        {
            IdEntrada = entrada.IdEntrada,
            EmailPropietarioActual = entrada.EmailPropietarioActual,
            TransferenciasRestantes = entrada.TransferenciasRestantes,
            Eventos = eventos
        };
    }

    private const string BaseEntradaSql = """
        SELECT
            e.id_entrada,
            e.fecha_hora,
            e.estado::text AS estado_entrada,
            e.codigo_qr,
            e.costo_total,
            e.transferencias_restantes,
            e.id_compra,
            c.estado::text AS estado_compra,
            e.nombre_sector::text AS nombre_sector,
            e.email_propietario_actual,
            u.nombre AS nombre_propietario,
            p.id_partido,
            p.fecha AS fecha_partido,
            p.hora AS hora_partido,
            p.equipo_local,
            p.equipo_visitante,
            p.fase::text AS fase,
            p.estado::text AS estado_partido,
            est.id_estadio,
            est.nombre_estadio,
            est.ciudad,
            est.pais::text AS pais_estadio
        FROM entrada e
        INNER JOIN compra c ON c.id_compra = e.id_compra
        INNER JOIN usuario u ON u.email = e.email_propietario_actual
        INNER JOIN partido p ON p.id_partido = e.id_partido
        INNER JOIN estadio est ON est.id_estadio = e.id_estadio
        """;

    private static EntradaDetalleResponse MapEntrada(NpgsqlDataReader reader)
    {
        return new EntradaDetalleResponse
        {
            IdEntrada = reader.GetInt32(reader.GetOrdinal("id_entrada")),
            FechaHora = reader.GetDateTime(reader.GetOrdinal("fecha_hora")),
            Estado = reader.GetString(reader.GetOrdinal("estado_entrada")),
            CodigoQr = reader.IsDBNull(reader.GetOrdinal("codigo_qr"))
                ? null
                : reader.GetString(reader.GetOrdinal("codigo_qr")),
            CostoTotal = reader.GetInt32(reader.GetOrdinal("costo_total")),
            TransferenciasRestantes = reader.GetInt32(reader.GetOrdinal("transferencias_restantes")),
            IdCompra = reader.GetInt32(reader.GetOrdinal("id_compra")),
            EstadoCompra = reader.GetString(reader.GetOrdinal("estado_compra")),
            NombreSector = reader.GetString(reader.GetOrdinal("nombre_sector")),
            EmailPropietarioActual = reader.GetString(reader.GetOrdinal("email_propietario_actual")),
            NombrePropietarioActual = reader.GetString(reader.GetOrdinal("nombre_propietario")),
            Partido = new PartidoEntradaDetalleResponse
            {
                IdPartido = reader.GetInt32(reader.GetOrdinal("id_partido")),
                Fecha = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha_partido")),
                Hora = reader.GetFieldValue<TimeOnly>(reader.GetOrdinal("hora_partido")),
                EquipoLocal = reader.GetString(reader.GetOrdinal("equipo_local")),
                EquipoVisitante = reader.GetString(reader.GetOrdinal("equipo_visitante")),
                Fase = reader.GetString(reader.GetOrdinal("fase")),
                Estado = reader.GetString(reader.GetOrdinal("estado_partido")),
                Estadio = new EstadioEntradaDetalleResponse
                {
                    IdEstadio = reader.GetInt32(reader.GetOrdinal("id_estadio")),
                    Nombre = reader.GetString(reader.GetOrdinal("nombre_estadio")),
                    Ciudad = reader.IsDBNull(reader.GetOrdinal("ciudad"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("ciudad")),
                    Pais = reader.GetString(reader.GetOrdinal("pais_estadio"))
                }
            }
        };
    }
}
