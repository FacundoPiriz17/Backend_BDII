using System.Text;
using Backend_BDII.Common.Database;
using Backend_BDII.Modules.Transferencias.DTOs;
using Npgsql;

namespace Backend_BDII.Modules.Transferencias.Repositories;

public sealed class TransferenciaRepository : ITransferenciaRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TransferenciaRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<TransferenciaResponse> CrearAsync(
        string emailOrigen,
        string emailDestino,
        int idEntrada,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            INSERT INTO transferencia (
                email_origen,
                email_destino,
                id_entrada
            )
            SELECT
                @email_origen,
                @email_destino,
                @id_entrada
            WHERE EXISTS (
                SELECT 1
                FROM entrada e
                INNER JOIN compra c ON c.id_compra = e.id_compra
                INNER JOIN partido p ON p.id_partido = e.id_partido
                WHERE e.id_entrada = @id_entrada
                  AND LOWER(e.email_propietario_actual) = LOWER(@email_origen)
                  AND e.estado = 'activa'
                  AND e.transferencias_restantes > 0
                  AND c.estado = 'paga'
                  AND p.estado = 'no empezado'
                  AND (p.fecha > CURRENT_DATE OR (p.fecha = CURRENT_DATE AND p.hora > LOCALTIME))
            )
            RETURNING id_transferencia;
            """;

        int? idTransferencia;

        await using (var command = new NpgsqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("email_origen", emailOrigen);
            command.Parameters.AddWithValue("email_destino", emailDestino);
            command.Parameters.AddWithValue("id_entrada", idEntrada);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            idTransferencia = result is null ? null : Convert.ToInt32(result);
        }

        if (idTransferencia is null)
            throw new InvalidOperationException("No se puede transferir: la entrada no existe, no está activa, no es tuya, no le quedan transferencias, su compra no está paga, o el partido ya empezó/terminó.");
        
        return await GetByIdUsingConnectionAsync(connection, idTransferencia.Value, emailOrigen, cancellationToken)
               ?? throw new InvalidOperationException("La transferencia fue creada, pero no se pudo recuperar.");
    }

    public async Task<List<TransferenciaResponse>> GetByUsuarioAsync(
        string emailUsuario,
        string relacion,
        string? estado,
        int? idEntrada,
        string? busqueda,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var sql = new StringBuilder(BaseSelectSql);
        sql.AppendLine();

        sql.AppendLine(relacion switch
        {
            "enviadas" => "WHERE LOWER(t.email_origen) = LOWER(@email_usuario)",
            "recibidas" => "WHERE LOWER(t.email_destino) = LOWER(@email_usuario)",
            _ => "WHERE (LOWER(t.email_origen) = LOWER(@email_usuario) OR LOWER(t.email_destino) = LOWER(@email_usuario))"
        });

        if (!string.IsNullOrWhiteSpace(estado))
            sql.AppendLine("  AND t.estado = CAST(@estado AS estado_transferencia_enum)");

        if (idEntrada.HasValue)
            sql.AppendLine("  AND t.id_entrada = @id_entrada");

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            sql.AppendLine("""
                  AND (
                    t.email_origen ILIKE @busqueda
                    OR t.email_destino ILIKE @busqueda
                    OR p.equipo_local ILIKE @busqueda
                    OR p.equipo_visitante ILIKE @busqueda
                    OR est.nombre_estadio ILIKE @busqueda
                  )
                """);
        }

        sql.AppendLine("ORDER BY t.fecha_hora DESC, t.id_transferencia DESC;");

        await using var command = new NpgsqlCommand(sql.ToString(), connection);
        command.Parameters.AddWithValue("email_usuario", emailUsuario);

        if (!string.IsNullOrWhiteSpace(estado))
            command.Parameters.AddWithValue("estado", estado);

        if (idEntrada.HasValue)
            command.Parameters.AddWithValue("id_entrada", idEntrada.Value);

        if (!string.IsNullOrWhiteSpace(busqueda))
            command.Parameters.AddWithValue("busqueda", $"%{busqueda.Trim()}%");

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var transferencias = new List<TransferenciaResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            transferencias.Add(MapTransferencia(reader));
        }

        return transferencias;
    }

    public async Task<TransferenciaResponse?> GetByIdAsync(
        int idTransferencia,
        string emailUsuario,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        return await GetByIdUsingConnectionAsync(connection, idTransferencia, emailUsuario, cancellationToken);
    }

   public async Task<TransferenciaResponse?> ActualizarEstadoAsync(
    int idTransferencia,
    string emailUsuario,
    string rolUsuario,
    string nuevoEstado,
    CancellationToken cancellationToken = default)
{
    await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

    try
    {
        var columnaUsuario = rolUsuario == "destino" ? "email_destino" : "email_origen";

        var updateTransferenciaSql = $"""
            UPDATE transferencia
            SET estado = CAST(@estado AS estado_transferencia_enum)
            WHERE id_transferencia = @id_transferencia
              AND LOWER({columnaUsuario}) = LOWER(@email_usuario)
              AND estado = 'pendiente'
            RETURNING id_transferencia;
            """;

        int? idActualizado;

        await using (var command = new NpgsqlCommand(updateTransferenciaSql, connection, transaction))
        {
            command.Parameters.AddWithValue("id_transferencia", idTransferencia);
            command.Parameters.AddWithValue("email_usuario", emailUsuario);
            command.Parameters.AddWithValue("estado", nuevoEstado);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            idActualizado = result is null ? null : Convert.ToInt32(result);
        }

        if (idActualizado is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        await transaction.CommitAsync(cancellationToken);

        return await GetByIdUsingConnectionAsync(connection, idTransferencia, emailUsuario, cancellationToken);
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
}

    public async Task<bool> TieneTransferenciaPendienteAsync(int idEntrada, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM transferencia
                WHERE id_entrada = @id_entrada
                  AND estado = 'pendiente'
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id_entrada", idEntrada);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool exists && exists;
    }

    private static async Task<TransferenciaResponse?> GetByIdUsingConnectionAsync(
        NpgsqlConnection connection,
        int idTransferencia,
        string emailUsuario,
        CancellationToken cancellationToken)
    {
        var sql = BaseSelectSql + "\n" + """
            WHERE t.id_transferencia = @id_transferencia
              AND (
                    LOWER(t.email_origen) = LOWER(@email_usuario)
                    OR LOWER(t.email_destino) = LOWER(@email_usuario)
                  );
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id_transferencia", idTransferencia);
        command.Parameters.AddWithValue("email_usuario", emailUsuario);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return MapTransferencia(reader);
    }

    private const string BaseSelectSql = """
        SELECT
            t.id_transferencia,
            t.fecha_hora,
            t.email_origen,
            t.email_destino,
            t.estado::text AS estado_transferencia,
            e.id_entrada,
            e.estado::text AS estado_entrada,
            e.costo_total,
            e.transferencias_restantes,
            e.nombre_sector::text AS nombre_sector,
            e.email_propietario_actual,
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
        FROM transferencia t
        INNER JOIN entrada e ON e.id_entrada = t.id_entrada
        INNER JOIN partido p ON p.id_partido = e.id_partido
        INNER JOIN estadio est ON est.id_estadio = e.id_estadio
        """;

    private static TransferenciaResponse MapTransferencia(NpgsqlDataReader reader)
    {
        return new TransferenciaResponse
        {
            IdTransferencia = reader.GetInt32(reader.GetOrdinal("id_transferencia")),
            FechaHora = reader.GetDateTime(reader.GetOrdinal("fecha_hora")),
            EmailOrigen = reader.GetString(reader.GetOrdinal("email_origen")),
            EmailDestino = reader.GetString(reader.GetOrdinal("email_destino")),
            Estado = reader.GetString(reader.GetOrdinal("estado_transferencia")),
            Entrada = new EntradaTransferenciaResponse
            {
                IdEntrada = reader.GetInt32(reader.GetOrdinal("id_entrada")),
                Estado = reader.GetString(reader.GetOrdinal("estado_entrada")),
                CostoTotal = reader.GetInt32(reader.GetOrdinal("costo_total")),
                TransferenciasRestantes = reader.GetInt32(reader.GetOrdinal("transferencias_restantes")),
                NombreSector = reader.GetString(reader.GetOrdinal("nombre_sector")),
                EmailPropietarioActual = reader.GetString(reader.GetOrdinal("email_propietario_actual")),
                Partido = new PartidoTransferenciaResponse
                {
                    IdPartido = reader.GetInt32(reader.GetOrdinal("id_partido")),
                    Fecha = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha_partido")),
                    Hora = reader.GetFieldValue<TimeOnly>(reader.GetOrdinal("hora_partido")),
                    EquipoLocal = reader.GetString(reader.GetOrdinal("equipo_local")),
                    EquipoVisitante = reader.GetString(reader.GetOrdinal("equipo_visitante")),
                    Fase = reader.GetString(reader.GetOrdinal("fase")),
                    Estado = reader.GetString(reader.GetOrdinal("estado_partido")),
                    Estadio = new EstadioTransferenciaResponse
                    {
                        IdEstadio = reader.GetInt32(reader.GetOrdinal("id_estadio")),
                        Nombre = reader.GetString(reader.GetOrdinal("nombre_estadio")),
                        Ciudad = reader.IsDBNull(reader.GetOrdinal("ciudad"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("ciudad")),
                        Pais = reader.GetString(reader.GetOrdinal("pais_estadio"))
                    }
                }
            }
        };
    }
}
