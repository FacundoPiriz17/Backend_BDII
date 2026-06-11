using System.Text;
using Backend_BDII.Common.Database;
using Backend_BDII.Modules.Validaciones.DTOs;
using Npgsql;

namespace Backend_BDII.Modules.Validaciones.Repositories;

public sealed class ValidacionRepository : IValidacionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ValidacionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ValidacionResponse> RegistrarAsync(
        string emailFuncionario,
        int idDispositivo,
        int idEntrada,
        string codigoEscaneado,
        string estado,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        if (!await DispositivoPerteneceAFuncionarioAsync(connection, idDispositivo, emailFuncionario, cancellationToken))
            throw new InvalidOperationException("El dispositivo no existe, no esta activo o no esta asignado al funcionario.");

        const string insertSql = """
            INSERT INTO valida (
                id_entrada,
                id_dispositivo,
                estado,
                codigo_escaneado
            )
            VALUES (
                @id_entrada,
                @id_dispositivo,
                CAST(@estado AS estado_validacion_enum),
                @codigo_escaneado
            )
            RETURNING id_validacion;
            """;

        int idValidacion;

        await using (var command = new NpgsqlCommand(insertSql, connection))
        {
            command.Parameters.AddWithValue("id_entrada", idEntrada);
            command.Parameters.AddWithValue("id_dispositivo", idDispositivo);
            command.Parameters.AddWithValue("estado", estado);
            command.Parameters.AddWithValue("codigo_escaneado", codigoEscaneado);

            idValidacion = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        }

        return await GetByIdUsingConnectionAsync(connection, idValidacion, cancellationToken)
               ?? throw new InvalidOperationException("La validacion fue registrada, pero no se pudo recuperar.");
    }

    public async Task<string?> GetCodigoQrActualAsync(int idEntrada, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT codigo_qr
            FROM entrada
            WHERE id_entrada = @id_entrada;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id_entrada", idEntrada);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is DBNull or null ? null : (string)result;
    }

    public async Task<List<ValidacionResponse>> GetHistorialAsync(
        string? emailFuncionario,
        int? idPartido,
        int? idEntrada,
        string? estado,
        int limit,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var sql = new StringBuilder(BaseSelectSql);
        sql.AppendLine();
        sql.AppendLine("WHERE 1 = 1");

        if (!string.IsNullOrWhiteSpace(emailFuncionario))
            sql.AppendLine("AND LOWER(d.email_funcionario) = LOWER(@email_funcionario)");

        if (idPartido.HasValue)
            sql.AppendLine("AND p.id_partido = @id_partido");

        if (idEntrada.HasValue)
            sql.AppendLine("AND e.id_entrada = @id_entrada");

        if (!string.IsNullOrWhiteSpace(estado))
            sql.AppendLine("AND v.estado = CAST(@estado AS estado_validacion_enum)");

        sql.AppendLine("ORDER BY v.fecha_hora DESC, v.id_validacion DESC");
        sql.AppendLine("LIMIT @limit;");

        await using var command = new NpgsqlCommand(sql.ToString(), connection);
        command.Parameters.AddWithValue("limit", limit);

        if (!string.IsNullOrWhiteSpace(emailFuncionario))
            command.Parameters.AddWithValue("email_funcionario", emailFuncionario);

        if (idPartido.HasValue)
            command.Parameters.AddWithValue("id_partido", idPartido.Value);

        if (idEntrada.HasValue)
            command.Parameters.AddWithValue("id_entrada", idEntrada.Value);

        if (!string.IsNullOrWhiteSpace(estado))
            command.Parameters.AddWithValue("estado", estado);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var validaciones = new List<ValidacionResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            validaciones.Add(MapValidacion(reader));
        }

        return validaciones;
    }

    public async Task<ValidacionResponse?> GetByIdAsync(int idValidacion, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        return await GetByIdUsingConnectionAsync(connection, idValidacion, cancellationToken);
    }

    public async Task<VerificacionManualResponse?> VerificarEntradaManualAsync(
        int idEntrada,
        int numeroDocumento,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var sql = BaseEntradaSelectSql + "\n" + """
            WHERE e.id_entrada = @id_entrada;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id_entrada", idEntrada);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        var entrada = MapEntrada(reader);

        return new VerificacionManualResponse
        {
            DocumentoCoincide = entrada.NumeroDocumento == numeroDocumento,
            Entrada = entrada
        };
    }

    private static async Task<bool> DispositivoPerteneceAFuncionarioAsync(
        NpgsqlConnection connection,
        int idDispositivo,
        string emailFuncionario,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM dispositivo_escaneo
                WHERE id_dispositivo_escaneo = @id_dispositivo
                  AND activo = TRUE
                  AND LOWER(email_funcionario) = LOWER(@email_funcionario)
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id_dispositivo", idDispositivo);
        command.Parameters.AddWithValue("email_funcionario", emailFuncionario);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool exists && exists;
    }

    private static async Task<ValidacionResponse?> GetByIdUsingConnectionAsync(
        NpgsqlConnection connection,
        int idValidacion,
        CancellationToken cancellationToken)
    {
        var sql = BaseSelectSql + "\n" + """
            WHERE v.id_validacion = @id_validacion;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id_validacion", idValidacion);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return MapValidacion(reader);
    }

    private const string BaseSelectSql = """
        SELECT
            v.id_validacion,
            v.id_entrada AS id_entrada_validacion,
            v.id_dispositivo,
            v.estado::text AS estado_validacion,
            v.codigo_escaneado,
            v.fecha_hora AS fecha_hora_validacion,
            d.email_funcionario,
            f.numero_legajo,
            uf.nombre AS nombre_funcionario,
            e.id_entrada,
            e.estado::text AS estado_entrada,
            e.costo_total,
            e.transferencias_restantes,
            e.nombre_sector::text AS nombre_sector,
            e.email_propietario_actual,
            up.nombre AS nombre_propietario,
            up.pais_documento,
            up.tipo_documento,
            up.numero_documento,
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
        FROM valida v
        INNER JOIN dispositivo_escaneo d ON d.id_dispositivo_escaneo = v.id_dispositivo
        INNER JOIN funcionario f ON f.email_funcionario = d.email_funcionario
        INNER JOIN usuario uf ON uf.email = f.email_funcionario
        INNER JOIN entrada e ON e.id_entrada = v.id_entrada
        INNER JOIN usuario up ON up.email = e.email_propietario_actual
        INNER JOIN partido p ON p.id_partido = e.id_partido
        INNER JOIN estadio est ON est.id_estadio = e.id_estadio
        """;

    private const string BaseEntradaSelectSql = """
        SELECT
            e.id_entrada,
            e.estado::text AS estado_entrada,
            e.costo_total,
            e.transferencias_restantes,
            e.nombre_sector::text AS nombre_sector,
            e.email_propietario_actual,
            up.nombre AS nombre_propietario,
            up.pais_documento,
            up.tipo_documento,
            up.numero_documento,
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
        INNER JOIN usuario up ON up.email = e.email_propietario_actual
        INNER JOIN partido p ON p.id_partido = e.id_partido
        INNER JOIN estadio est ON est.id_estadio = e.id_estadio
        """;

    private static ValidacionResponse MapValidacion(NpgsqlDataReader reader)
    {
        return new ValidacionResponse
        {
            IdValidacion = reader.GetInt32(reader.GetOrdinal("id_validacion")),
            IdEntrada = reader.GetInt32(reader.GetOrdinal("id_entrada_validacion")),
            IdDispositivo = reader.GetInt32(reader.GetOrdinal("id_dispositivo")),
            Estado = reader.GetString(reader.GetOrdinal("estado_validacion")),
            CodigoEscaneado = reader.GetString(reader.GetOrdinal("codigo_escaneado")),
            FechaHora = reader.GetDateTime(reader.GetOrdinal("fecha_hora_validacion")),
            Funcionario = new FuncionarioValidacionResponse
            {
                Email = reader.GetString(reader.GetOrdinal("email_funcionario")),
                Nombre = reader.GetString(reader.GetOrdinal("nombre_funcionario")),
                NumeroLegajo = reader.GetInt32(reader.GetOrdinal("numero_legajo"))
            },
            Entrada = MapEntrada(reader)
        };
    }

    private static EntradaValidacionResponse MapEntrada(NpgsqlDataReader reader)
    {
        return new EntradaValidacionResponse
        {
            IdEntrada = reader.GetInt32(reader.GetOrdinal("id_entrada")),
            Estado = reader.GetString(reader.GetOrdinal("estado_entrada")),
            CostoTotal = reader.GetInt32(reader.GetOrdinal("costo_total")),
            TransferenciasRestantes = reader.GetInt32(reader.GetOrdinal("transferencias_restantes")),
            NombreSector = reader.GetString(reader.GetOrdinal("nombre_sector")),
            EmailPropietarioActual = reader.GetString(reader.GetOrdinal("email_propietario_actual")),
            NombrePropietarioActual = reader.GetString(reader.GetOrdinal("nombre_propietario")),
            PaisDocumento = reader.GetString(reader.GetOrdinal("pais_documento")),
            TipoDocumento = reader.GetString(reader.GetOrdinal("tipo_documento")),
            NumeroDocumento = reader.GetInt32(reader.GetOrdinal("numero_documento")),
            Partido = new PartidoValidacionResponse
            {
                IdPartido = reader.GetInt32(reader.GetOrdinal("id_partido")),
                Fecha = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha_partido")),
                Hora = reader.GetFieldValue<TimeOnly>(reader.GetOrdinal("hora_partido")),
                EquipoLocal = reader.GetString(reader.GetOrdinal("equipo_local")),
                EquipoVisitante = reader.GetString(reader.GetOrdinal("equipo_visitante")),
                Fase = reader.GetString(reader.GetOrdinal("fase")),
                Estado = reader.GetString(reader.GetOrdinal("estado_partido")),
                Estadio = new EstadioValidacionResponse
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
