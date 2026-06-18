using System.Text;
using Backend_BDII.Common.Database;
using Backend_BDII.Modules.Compras.DTOs;
using Npgsql;

namespace Backend_BDII.Modules.Compras.Repositories;

public sealed class CompraRepository : ICompraRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CompraRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<CompraResponse> CrearAsync(
        string emailUsuario,
        IReadOnlyCollection<NuevaEntradaCompra> entradas,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string insertCompraSql = """
                INSERT INTO compra (email_usuario)
                VALUES (@email_usuario)
                RETURNING id_compra;
                """;

            int idCompra;

            await using (var command = new NpgsqlCommand(insertCompraSql, connection, transaction))
            {
                command.Parameters.AddWithValue("email_usuario", emailUsuario);
                idCompra = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            }

            foreach (var entrada in entradas)
            {
                var idEstadio = await GetIdEstadioParaEntradaAsync(
                    connection,
                    transaction,
                    entrada.IdPartido,
                    entrada.NombreSector,
                    cancellationToken);

                if (idEstadio is null)
                    throw new InvalidOperationException("El partido no existe, no esta disponible o el sector no esta habilitado.");

                const string insertEntradaSql = """
                    INSERT INTO entrada (
                        id_compra,
                        id_partido,
                        nombre_sector,
                        id_estadio,
                        codigo_qr
                    )
                    VALUES (
                        @id_compra,
                        @id_partido,
                        CAST(@nombre_sector AS sector_enum),
                        @id_estadio,
                        @codigo_qr
                    );
                    """;

                await using var command = new NpgsqlCommand(insertEntradaSql, connection, transaction);
                command.Parameters.AddWithValue("id_compra", idCompra);
                command.Parameters.AddWithValue("id_partido", entrada.IdPartido);
                command.Parameters.AddWithValue("nombre_sector", entrada.NombreSector);
                command.Parameters.AddWithValue("id_estadio", idEstadio.Value);
                command.Parameters.AddWithValue("codigo_qr", entrada.CodigoQr);

                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            var compra = await GetByIdUsingConnectionAsync(connection, idCompra, emailUsuario, transaction, cancellationToken)
                   ?? throw new InvalidOperationException("La compra fue creada, pero no se pudo recuperar.");

            await transaction.CommitAsync(cancellationToken);

            return compra;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<List<CompraResponse>> GetByUsuarioAsync(
        string emailUsuario,
        string? estado,
        int? idPartido,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var sql = new StringBuilder("""
            SELECT
                c.id_compra,
                c.fecha_hora,
                c.monto_total,
                c.porcentaje_comision,
                c.email_usuario,
                c.estado::text AS estado
            FROM compra c
            WHERE LOWER(c.email_usuario) = LOWER(@email_usuario)
            """);
        sql.AppendLine();

        if (!string.IsNullOrWhiteSpace(estado))
            sql.AppendLine("AND c.estado = CAST(@estado AS estado_compra_enum)");

        if (idPartido.HasValue)
        {
            sql.AppendLine("""
                AND EXISTS (
                    SELECT 1
                    FROM entrada e
                    WHERE e.id_compra = c.id_compra
                      AND e.id_partido = @id_partido
                )
                """);
        }

        sql.AppendLine("ORDER BY c.fecha_hora DESC, c.id_compra DESC;");

        await using var command = new NpgsqlCommand(sql.ToString(), connection);
        command.Parameters.AddWithValue("email_usuario", emailUsuario);

        if (!string.IsNullOrWhiteSpace(estado))
            command.Parameters.AddWithValue("estado", estado);

        if (idPartido.HasValue)
            command.Parameters.AddWithValue("id_partido", idPartido.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var compras = new List<CompraResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            compras.Add(MapCompra(reader, []));
        }

        await reader.CloseAsync();

        foreach (var compra in compras)
        {
            compra.Entradas.AddRange(await GetEntradasByCompraAsync(
                connection,
                compra.IdCompra,
                emailUsuario,
                null,
                cancellationToken));
        }

        return compras;
    }

    public async Task<CompraResponse?> GetByIdAsync(int idCompra, string emailUsuario, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        return await GetByIdUsingConnectionAsync(connection, idCompra, emailUsuario, null, cancellationToken);
    }

    public async Task<List<EntradaResponse>> GetEntradasAsignadasAsync(
        string emailUsuario,
        string? estado,
        int? idPartido,
        string? busqueda,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var sql = new StringBuilder("""
            SELECT
                e.id_entrada,
                e.fecha_hora,
                e.estado::text AS estado_entrada,
                e.codigo_qr,
                e.costo_total,
                e.transferencias_restantes,
                e.id_compra,
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
            FROM entrada e
            INNER JOIN compra c ON c.id_compra = e.id_compra
            INNER JOIN partido p ON p.id_partido = e.id_partido
            INNER JOIN estadio est ON est.id_estadio = e.id_estadio
            LEFT JOIN equipo el ON el.codigo_fifa = p.equipo_local
            LEFT JOIN equipo ev ON ev.codigo_fifa = p.equipo_visitante
            WHERE LOWER(e.email_propietario_actual) = LOWER(@email_usuario)
              AND c.estado = 'paga'
            """);
        sql.AppendLine();

        // Filtro de estado: si no se especifica, ocultamos las canceladas
        // (la billetera muestra entradas vigentes y ya consumidas).
        if (!string.IsNullOrWhiteSpace(estado))
            sql.AppendLine("AND e.estado = CAST(@estado AS estado_entrada_enum)");
        else
            sql.AppendLine("AND e.estado <> 'cancelada'");

        if (idPartido.HasValue)
            sql.AppendLine("AND p.id_partido = @id_partido");

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            sql.AppendLine("""
                AND (
                    p.equipo_local ILIKE @busqueda
                    OR p.equipo_visitante ILIKE @busqueda
                    OR el.nombre_equipo ILIKE @busqueda
                    OR ev.nombre_equipo ILIKE @busqueda
                    OR est.nombre_estadio ILIKE @busqueda
                    OR est.ciudad ILIKE @busqueda
                )
                """);
        }

        sql.AppendLine("ORDER BY p.fecha, p.hora, e.id_entrada;");

        await using var command = new NpgsqlCommand(sql.ToString(), connection);
        command.Parameters.AddWithValue("email_usuario", emailUsuario);

        if (!string.IsNullOrWhiteSpace(estado))
            command.Parameters.AddWithValue("estado", estado);

        if (idPartido.HasValue)
            command.Parameters.AddWithValue("id_partido", idPartido.Value);

        if (!string.IsNullOrWhiteSpace(busqueda))
            command.Parameters.AddWithValue("busqueda", $"%{busqueda.Trim()}%");

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var entradas = new List<EntradaResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            entradas.Add(MapEntrada(reader));
        }

        return entradas;
    }

    public async Task<List<PartidoDisponibleResponse>> GetPartidosDisponiblesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                p.id_partido,
                p.fecha AS fecha_partido,
                p.hora AS hora_partido,
                p.equipo_local,
                p.equipo_visitante,
                p.fase::text AS fase,
                p.estado::text AS estado_partido,
                p.costo AS costo_base,
                est.id_estadio,
                est.nombre_estadio,
                est.ciudad,
                est.pais::text AS pais_estadio,
                ps.nombre_sector::text AS nombre_sector,
                COALESCE(s.capacidad, 0) AS capacidad,
                COALESCE(s.costo, 0) AS costo_sector,
                COUNT(e.id_entrada) FILTER (
                WHERE e.estado <> 'cancelada'
                )::int AS entradas_vendidas
            FROM partido p
            INNER JOIN estadio est ON est.id_estadio = p.id_estadio
            INNER JOIN partido_sector ps ON ps.id_partido = p.id_partido AND ps.id_estadio = p.id_estadio
            INNER JOIN sector s ON s.nombre_sector = ps.nombre_sector AND s.id_estadio = ps.id_estadio
            LEFT JOIN entrada e ON e.id_partido = p.id_partido
                AND e.id_estadio = ps.id_estadio
                AND e.nombre_sector = ps.nombre_sector
            WHERE p.estado = 'no empezado'
              AND (
                    p.fecha > CURRENT_DATE
                    OR (p.fecha = CURRENT_DATE AND p.hora > LOCALTIME)
                  )
            GROUP BY
                p.id_partido,
                p.fecha,
                p.hora,
                p.equipo_local,
                p.equipo_visitante,
                p.fase,
                p.estado,
                p.costo,
                est.id_estadio,
                est.nombre_estadio,
                est.ciudad,
                est.pais,
                ps.nombre_sector,
                s.capacidad,
                s.costo
            ORDER BY p.fecha, p.hora, p.id_partido, ps.nombre_sector;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var partidos = new Dictionary<int, PartidoDisponibleResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var idPartido = reader.GetInt32(reader.GetOrdinal("id_partido"));

            if (!partidos.TryGetValue(idPartido, out var partido))
            {
                partido = new PartidoDisponibleResponse
                {
                    IdPartido = idPartido,
                    Fecha = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha_partido")),
                    Hora = reader.GetFieldValue<TimeOnly>(reader.GetOrdinal("hora_partido")),
                    EquipoLocal = reader.GetString(reader.GetOrdinal("equipo_local")),
                    EquipoVisitante = reader.GetString(reader.GetOrdinal("equipo_visitante")),
                    Fase = reader.GetString(reader.GetOrdinal("fase")),
                    Estado = reader.GetString(reader.GetOrdinal("estado_partido")),
                    CostoBase = reader.GetInt32(reader.GetOrdinal("costo_base")),
                    Estadio = new EstadioEntradaResponse
                    {
                        IdEstadio = reader.GetInt32(reader.GetOrdinal("id_estadio")),
                        Nombre = reader.GetString(reader.GetOrdinal("nombre_estadio")),
                        Ciudad = reader.IsDBNull(reader.GetOrdinal("ciudad"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("ciudad")),
                        Pais = reader.GetString(reader.GetOrdinal("pais_estadio"))
                    },
                    Sectores = []
                };

                partidos.Add(idPartido, partido);
            }

            var capacidad = reader.GetInt32(reader.GetOrdinal("capacidad"));
            var entradasVendidas = reader.GetInt32(reader.GetOrdinal("entradas_vendidas"));
            var costoSector = reader.GetInt32(reader.GetOrdinal("costo_sector"));

            partido.Sectores.Add(new SectorDisponibleResponse
            {
                NombreSector = reader.GetString(reader.GetOrdinal("nombre_sector")),
                Capacidad = capacidad,
                EntradasVendidas = entradasVendidas,
                EntradasDisponibles = Math.Max(0, capacidad - entradasVendidas),
                CostoSector = costoSector,
                CostoTotalEntrada = partido.CostoBase + costoSector
            });
        }

        return partidos.Values.ToList();
    }

    public async Task<CompraResponse?> ActualizarEstadoAsync(
        int idCompra,
        string emailUsuario,
        string nuevoEstado,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            UPDATE compra
            SET estado = CAST(@estado AS estado_compra_enum)
            WHERE id_compra = @id_compra
              AND LOWER(email_usuario) = LOWER(@email_usuario);
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id_compra", idCompra);
        command.Parameters.AddWithValue("email_usuario", emailUsuario);
        command.Parameters.AddWithValue("estado", nuevoEstado);

        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);

        if (affectedRows == 0)
            return null;

        return await GetByIdUsingConnectionAsync(connection, idCompra, emailUsuario, null, cancellationToken);
    }

    public async Task<CompraResponse?> CancelarAsync(
    int idCompra,
    string emailUsuario,
    CancellationToken cancellationToken = default)
{
    await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

    try
    {
        const string cancelarEntradasSql = """
            UPDATE entrada e
            SET estado = 'cancelada'
            FROM compra c
            WHERE c.id_compra = e.id_compra
              AND c.id_compra = @id_compra
              AND LOWER(c.email_usuario) = LOWER(@email_usuario)
              AND c.estado IN ('pendiente', 'confirmada')
              AND e.estado = 'activa';
            """;

        await using (var command = new NpgsqlCommand(cancelarEntradasSql, connection, transaction))
        {
            command.Parameters.AddWithValue("id_compra", idCompra);
            command.Parameters.AddWithValue("email_usuario", emailUsuario);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        const string updateCompraSql = """
            UPDATE compra
            SET estado = 'cancelada'
            WHERE id_compra = @id_compra
              AND LOWER(email_usuario) = LOWER(@email_usuario)
              AND estado IN ('pendiente', 'confirmada');
            """;

        int affectedRows;

        await using (var command = new NpgsqlCommand(updateCompraSql, connection, transaction))
        {
            command.Parameters.AddWithValue("id_compra", idCompra);
            command.Parameters.AddWithValue("email_usuario", emailUsuario);
            affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        }

        if (affectedRows == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var compra = await GetByIdUsingConnectionAsync(
            connection,
            idCompra,
            emailUsuario,
            transaction,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return compra;
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
}

    public async Task<string?> ActualizarQrEntradaAsync(
        int idEntrada,
        string emailUsuario,
        string codigoQr,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            UPDATE entrada e
            SET codigo_qr = @codigo_qr
            FROM compra c
            WHERE c.id_compra = e.id_compra
              AND e.id_entrada = @id_entrada
              AND LOWER(e.email_propietario_actual) = LOWER(@email_usuario)
              AND e.estado = 'activa'
              AND c.estado = 'paga'
            RETURNING e.codigo_qr;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id_entrada", idEntrada);
        command.Parameters.AddWithValue("email_usuario", emailUsuario);
        command.Parameters.AddWithValue("codigo_qr", codigoQr);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result as string;
    }

    private static async Task<int?> GetIdEstadioParaEntradaAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int idPartido,
        string nombreSector,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT p.id_estadio
            FROM partido p
            INNER JOIN partido_sector ps ON ps.id_partido = p.id_partido AND ps.id_estadio = p.id_estadio
            WHERE p.id_partido = @id_partido
              AND ps.nombre_sector = CAST(@nombre_sector AS sector_enum)
              AND p.estado = 'no empezado'
              AND (
                    p.fecha > CURRENT_DATE
                    OR (p.fecha = CURRENT_DATE AND p.hora > LOCALTIME)
                  );
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id_partido", idPartido);
        command.Parameters.AddWithValue("nombre_sector", nombreSector);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null ? null : Convert.ToInt32(result);
    }

    private static async Task<CompraResponse?> GetByIdUsingConnectionAsync(
        NpgsqlConnection connection,
        int idCompra,
        string emailUsuario,
        NpgsqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                c.id_compra,
                c.fecha_hora,
                c.monto_total,
                c.porcentaje_comision,
                c.email_usuario,
                c.estado::text AS estado
            FROM compra c
            WHERE c.id_compra = @id_compra
              AND LOWER(c.email_usuario) = LOWER(@email_usuario);
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id_compra", idCompra);
        command.Parameters.AddWithValue("email_usuario", emailUsuario);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        var compra = MapCompra(reader, []);
        await reader.CloseAsync();

        compra.Entradas.AddRange(await GetEntradasByCompraAsync(
            connection,
            idCompra,
            emailUsuario,
            transaction,
            cancellationToken));

        return compra;
    }

    private static async Task<List<EntradaResponse>> GetEntradasByCompraAsync(
        NpgsqlConnection connection,
        int idCompra,
        string emailUsuario,
        NpgsqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                e.id_entrada,
                e.fecha_hora,
                e.estado::text AS estado_entrada,
                CASE
                    WHEN LOWER(e.email_propietario_actual) = LOWER(@email_usuario)
                    THEN e.codigo_qr
                    ELSE NULL
                END AS codigo_qr,
                e.costo_total,
                e.transferencias_restantes,
                e.id_compra,
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
            FROM entrada e
            INNER JOIN partido p ON p.id_partido = e.id_partido
            INNER JOIN estadio est ON est.id_estadio = e.id_estadio
            WHERE e.id_compra = @id_compra
            ORDER BY e.id_entrada;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id_compra", idCompra);
        command.Parameters.AddWithValue("email_usuario", emailUsuario);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var entradas = new List<EntradaResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            entradas.Add(MapEntrada(reader));
        }

        return entradas;
    }

    private static CompraResponse MapCompra(NpgsqlDataReader reader, List<EntradaResponse> entradas)
    {
        return new CompraResponse
        {
            IdCompra = reader.GetInt32(reader.GetOrdinal("id_compra")),
            FechaHora = reader.GetDateTime(reader.GetOrdinal("fecha_hora")),
            MontoTotal = reader.GetInt32(reader.GetOrdinal("monto_total")),
            PorcentajeComision = reader.GetDouble(reader.GetOrdinal("porcentaje_comision")),
            EmailUsuario = reader.GetString(reader.GetOrdinal("email_usuario")),
            Estado = reader.GetString(reader.GetOrdinal("estado")),
            Entradas = entradas
        };
    }

    private static EntradaResponse MapEntrada(NpgsqlDataReader reader)
    {
        return new EntradaResponse
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
            NombreSector = reader.GetString(reader.GetOrdinal("nombre_sector")),
            EmailPropietarioActual = reader.GetString(reader.GetOrdinal("email_propietario_actual")),
            Partido = new PartidoEntradaResponse
            {
                IdPartido = reader.GetInt32(reader.GetOrdinal("id_partido")),
                Fecha = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha_partido")),
                Hora = reader.GetFieldValue<TimeOnly>(reader.GetOrdinal("hora_partido")),
                EquipoLocal = reader.GetString(reader.GetOrdinal("equipo_local")),
                EquipoVisitante = reader.GetString(reader.GetOrdinal("equipo_visitante")),
                Fase = reader.GetString(reader.GetOrdinal("fase")),
                Estado = reader.GetString(reader.GetOrdinal("estado_partido")),
                Estadio = new EstadioEntradaResponse
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
