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

    public async Task<List<EntradasResponse>> GetByUsuarioAsync(
        string emailUsuario,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                id_entrada,
                fecha_hora,
                estado::text AS estado,
                codigo_qr,
                costo_total,
                transferencias_restantes,
                id_compra,
                id_partido,
                nombre_sector::text AS nombre_sector,
                id_estadio,
                email_propietario_actual
            FROM entrada
            WHERE LOWER(email_propietario_actual) = LOWER(@email_usuario)
            ORDER BY fecha_hora DESC;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email_usuario", emailUsuario);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var entradas = new List<EntradasResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            entradas.Add(MapEntrada(reader));
        }

        return entradas;
    }

    public async Task<EntradasResponse?> GetByIdAsync(
        int idEntrada,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                id_entrada,
                fecha_hora,
                estado::text AS estado,
                codigo_qr,
                costo_total,
                transferencias_restantes,
                id_compra,
                id_partido,
                nombre_sector::text AS nombre_sector,
                id_estadio,
                email_propietario_actual
            FROM entrada
            WHERE id_entrada = @id_entrada
            """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id_entrada", idEntrada);


        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return MapEntrada(reader);
    }
    

    public async Task<EntradasResponse?> ActualizarEstadoAsync(
        int idEntrada,
        string nuevoEstado,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            UPDATE entrada
            SET estado = CAST(@estado AS estado_entrada_enum)
            WHERE id_entrada = @id_entrada
            """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id_entrada", idEntrada);
        command.Parameters.AddWithValue("estado", nuevoEstado);

        var affectedRows =
            await command.ExecuteNonQueryAsync(cancellationToken);

        if (affectedRows == 0)
            return null;

        return await GetByIdAsync(
            idEntrada,
            cancellationToken);
    }

    private static EntradasResponse MapEntrada(
        NpgsqlDataReader reader)
    {
        return new EntradasResponse
        {
            idEntrada = reader.GetInt32(reader.GetOrdinal("id_entrada")),
            fechaHora = reader.GetDateTime(reader.GetOrdinal("fecha_hora")),
            estado = reader.GetString(reader.GetOrdinal("estado")),
            codigoQr = reader.IsDBNull(reader.GetOrdinal("codigo_qr"))
                ? ""
                : reader.GetString(reader.GetOrdinal("codigo_qr")),
            costoTotal = reader.GetInt32(reader.GetOrdinal("costo_total")),
            transferenciasRestantes = reader.GetInt32(reader.GetOrdinal("transferencias_restantes")),
            idCompra = reader.GetInt32(reader.GetOrdinal("id_compra")),
            idPartido = reader.GetInt32(reader.GetOrdinal("id_partido")),
            nombreSector = reader.GetString(reader.GetOrdinal("nombre_sector")),
            idEstadio = reader.GetInt32(reader.GetOrdinal("id_estadio")),
            emailPropietarioActual = reader.GetString(reader.GetOrdinal("email_propietario_actual"))
        };
    }
}