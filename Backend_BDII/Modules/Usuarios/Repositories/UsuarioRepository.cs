using System.Text;
using Backend_BDII.Common.Database;
using Backend_BDII.Common.Domain;
using Backend_BDII.Modules.Usuarios.DTOs;
using Npgsql;

namespace Backend_BDII.Modules.Usuarios.Repositories;

public sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UsuarioRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<UsuarioResponse>> GetAllAsync(
        string? rol,
        bool? habilitado,
        string? busqueda,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var sql = new StringBuilder(BaseUsuarioSql);
        sql.AppendLine();
        sql.AppendLine("WHERE 1 = 1");

        if (!string.IsNullOrWhiteSpace(rol))
        {
            sql.AppendLine(rol.Trim().ToLowerInvariant() switch
            {
                "general" => "AND g.email_general IS NOT NULL",
                "admin" => "AND a.email_admin IS NOT NULL",
                "funcionario" => "AND f.email_funcionario IS NOT NULL",
                _ => "AND FALSE"
            });
        }

        if (habilitado.HasValue)
            sql.AppendLine("AND u.habilitado = @habilitado");

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            sql.AppendLine("""
                AND (
                    u.email ILIKE @busqueda
                    OR u.nombre ILIKE @busqueda
                    OR u.tipo_documento ILIKE @busqueda
                    OR u.numero_documento::text ILIKE @busqueda
                )
                """);
        }

        sql.AppendLine("ORDER BY u.email;");

        await using var command = new NpgsqlCommand(sql.ToString(), connection);

        if (habilitado.HasValue)
            command.Parameters.AddWithValue("habilitado", habilitado.Value);

        if (!string.IsNullOrWhiteSpace(busqueda))
            command.Parameters.AddWithValue("busqueda", $"%{busqueda.Trim()}%");

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var usuarios = new List<UsuarioResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            usuarios.Add(MapUsuario(reader));
        }

        return usuarios;
    }

    public async Task<UsuarioResponse?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        return await GetByEmailUsingConnectionAsync(connection, email, null, cancellationToken);
    }

    public async Task<MiPerfilResponse?> GetMiPerfilAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                u.email,
                u.nombre,
                u.pais_documento,
                u.tipo_documento,
                u.numero_documento,
                u.localidad_direccion,
                u.calle_direccion,
                u.pais_direccion,
                u.numero_direccion,
                u.codigo_postal_direccion,
                EXISTS (SELECT 1 FROM general g WHERE g.email_general = u.email) AS es_general,
                EXISTS (SELECT 1 FROM admin a WHERE a.email_admin = u.email) AS es_admin,
                EXISTS (SELECT 1 FROM funcionario f WHERE f.email_funcionario = u.email) AS es_funcionario
            FROM usuario u
            WHERE LOWER(u.email) = LOWER(@email);
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email", email);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        var roles = new List<string>();

        if (reader.GetBoolean(reader.GetOrdinal("es_general"))) roles.Add("General");
        if (reader.GetBoolean(reader.GetOrdinal("es_admin"))) roles.Add("Admin");
        if (reader.GetBoolean(reader.GetOrdinal("es_funcionario"))) roles.Add("Funcionario");

        var perfil = new MiPerfilResponse
        {
            Email = reader.GetString(reader.GetOrdinal("email")),
            Nombre = reader.GetString(reader.GetOrdinal("nombre")),
            PaisDocumento = reader.GetString(reader.GetOrdinal("pais_documento")),
            TipoDocumento = reader.GetString(reader.GetOrdinal("tipo_documento")),
            NumeroDocumento = reader.GetInt32(reader.GetOrdinal("numero_documento")),
            LocalidadDireccion = reader.IsDBNull(reader.GetOrdinal("localidad_direccion"))
                ? null
                : reader.GetString(reader.GetOrdinal("localidad_direccion")),
            CalleDireccion = reader.IsDBNull(reader.GetOrdinal("calle_direccion"))
                ? null
                : reader.GetString(reader.GetOrdinal("calle_direccion")),
            PaisDireccion = reader.IsDBNull(reader.GetOrdinal("pais_direccion"))
                ? null
                : reader.GetString(reader.GetOrdinal("pais_direccion")),
            NumeroDireccion = reader.IsDBNull(reader.GetOrdinal("numero_direccion"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("numero_direccion")),
            CodigoPostalDireccion = reader.IsDBNull(reader.GetOrdinal("codigo_postal_direccion"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("codigo_postal_direccion")),
            Telefonos = [],
            Roles = roles
        };

        await reader.CloseAsync();

        const string telefonosSql = """
            SELECT telefono
            FROM telefonos
            WHERE LOWER(email_usuario) = LOWER(@email)
            ORDER BY telefono;
            """;

        await using var telefonosCommand = new NpgsqlCommand(telefonosSql, connection);
        telefonosCommand.Parameters.AddWithValue("email", email);

        await using var telefonosReader = await telefonosCommand.ExecuteReaderAsync(cancellationToken);

        while (await telefonosReader.ReadAsync(cancellationToken))
        {
            perfil.Telefonos.Add(telefonosReader.GetString(telefonosReader.GetOrdinal("telefono")));
        }

        return perfil;
    }

    public async Task<UsuarioResponse> CrearAsync(
        CrearUsuarioAdminRequest request,
        string passwordHash,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string insertUsuarioSql = """
                INSERT INTO usuario (
                    email,
                    nombre,
                    habilitado,
                    pais_documento,
                    tipo_documento,
                    numero_documento,
                    localidad_direccion,
                    calle_direccion,
                    pais_direccion,
                    numero_direccion,
                    codigo_postal_direccion
                )
                VALUES (
                    @email,
                    @nombre,
                    @habilitado,
                    @pais_documento,
                    @tipo_documento,
                    @numero_documento,
                    @localidad_direccion,
                    @calle_direccion,
                    @pais_direccion,
                    @numero_direccion,
                    @codigo_postal_direccion
                );
                """;

            await using (var command = new NpgsqlCommand(insertUsuarioSql, connection, transaction))
            {
                AddUsuarioParameters(command, request);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            const string insertLoginSql = """
                INSERT INTO login (email, contrasena)
                VALUES (@email, @contrasena);
                """;

            await using (var command = new NpgsqlCommand(insertLoginSql, connection, transaction))
            {
                command.Parameters.AddWithValue("email", request.Email.Trim().ToLowerInvariant());
                command.Parameters.AddWithValue("contrasena", passwordHash);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await ReemplazarTelefonosAsync(connection, transaction, request.Email, request.Telefonos, cancellationToken);
            await AplicarRolesAsync(connection, transaction, request.Email, request.Roles, request.PaisAdmin, request.NumeroLegajo, request.EstadoVerificacion, cancellationToken);

            var usuario = await GetByEmailUsingConnectionAsync(connection, request.Email, transaction, cancellationToken)
                          ?? throw new InvalidOperationException("El usuario fue creado, pero no se pudo recuperar.");

            await transaction.CommitAsync(cancellationToken);
            return usuario;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<UsuarioResponse?> ActualizarAsync(
        string email,
        ActualizarUsuarioRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string updateSql = """
                UPDATE usuario
                SET nombre = @nombre,
                    habilitado = @habilitado,
                    pais_documento = @pais_documento,
                    tipo_documento = @tipo_documento,
                    numero_documento = @numero_documento,
                    localidad_direccion = @localidad_direccion,
                    calle_direccion = @calle_direccion,
                    pais_direccion = @pais_direccion,
                    numero_direccion = @numero_direccion,
                    codigo_postal_direccion = @codigo_postal_direccion
                WHERE LOWER(email) = LOWER(@email);
                """;

            int affectedRows;

            await using (var command = new NpgsqlCommand(updateSql, connection, transaction))
            {
                command.Parameters.AddWithValue("email", email.Trim().ToLowerInvariant());
                command.Parameters.AddWithValue("nombre", request.Nombre.Trim());
                command.Parameters.AddWithValue("habilitado", request.Habilitado);
                command.Parameters.AddWithValue("pais_documento", request.PaisDocumento.Trim());
                command.Parameters.AddWithValue("tipo_documento", request.TipoDocumento.Trim());
                command.Parameters.AddWithValue("numero_documento", request.NumeroDocumento);
                command.Parameters.AddWithValue("localidad_direccion", (object?)request.LocalidadDireccion?.Trim() ?? DBNull.Value);
                command.Parameters.AddWithValue("calle_direccion", (object?)request.CalleDireccion?.Trim() ?? DBNull.Value);
                command.Parameters.AddWithValue("pais_direccion", (object?)request.PaisDireccion?.Trim() ?? DBNull.Value);
                command.Parameters.AddWithValue("numero_direccion", (object?)request.NumeroDireccion ?? DBNull.Value);
                command.Parameters.AddWithValue("codigo_postal_direccion", (object?)request.CodigoPostalDireccion ?? DBNull.Value);
                affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
            }

            if (affectedRows == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            await ReemplazarTelefonosAsync(connection, transaction, email, request.Telefonos, cancellationToken);

            var usuario = await GetByEmailUsingConnectionAsync(connection, email, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return usuario;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<UsuarioResponse?> ActualizarHabilitacionAsync(
        string email,
        bool habilitado,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            UPDATE usuario
            SET habilitado = @habilitado
            WHERE LOWER(email) = LOWER(@email);
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email", email.Trim().ToLowerInvariant());
        command.Parameters.AddWithValue("habilitado", habilitado);

        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);

        if (affectedRows == 0)
            return null;

        return await GetByEmailUsingConnectionAsync(connection, email, null, cancellationToken);
    }

    public async Task<UsuarioResponse?> ActualizarRolesAsync(
        string email,
        ActualizarRolesUsuarioRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (!await ExisteUsuarioAsync(connection, transaction, email, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            await AplicarRolesAsync(connection, transaction, email, request.Roles, request.PaisAdmin, request.NumeroLegajo, request.EstadoVerificacion, cancellationToken);

            var usuario = await GetByEmailUsingConnectionAsync(connection, email, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return usuario;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task AplicarRolesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string email,
        IEnumerable<string> roles,
        string? paisAdmin,
        int? numeroLegajo,
        bool? estadoVerificacion,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var normalizedRoles = roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (normalizedRoles.Contains("admin"))
        {
            const string upsertAdminSql = """
                INSERT INTO admin (email_admin, pais)
                VALUES (@email, CAST(@pais AS pais_sede_enum))
                ON CONFLICT (email_admin) DO UPDATE
                SET pais = EXCLUDED.pais;
                """;

            await using var command = new NpgsqlCommand(upsertAdminSql, connection, transaction);
            command.Parameters.AddWithValue("email", normalizedEmail);
            command.Parameters.AddWithValue("pais", PaisSedeNormalizer.Normalize(paisAdmin ?? throw new InvalidOperationException("El pais sede es obligatorio para el rol Admin.")));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        else
        {
            await DeleteRoleAsync(connection, transaction, "admin", "email_admin", normalizedEmail, cancellationToken);
        }

        if (normalizedRoles.Contains("funcionario"))
        {
            const string upsertFuncionarioSql = """
                INSERT INTO funcionario (email_funcionario, numero_legajo)
                VALUES (@email, @numero_legajo)
                ON CONFLICT (email_funcionario) DO UPDATE
                SET numero_legajo = EXCLUDED.numero_legajo;
                """;

            await using var command = new NpgsqlCommand(upsertFuncionarioSql, connection, transaction);
            command.Parameters.AddWithValue("email", normalizedEmail);
            command.Parameters.AddWithValue("numero_legajo", numeroLegajo ?? throw new InvalidOperationException("El numero de legajo es obligatorio para el rol Funcionario."));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        else
        {
            await DeleteRoleAsync(connection, transaction, "funcionario", "email_funcionario", normalizedEmail, cancellationToken);
        }

        if (normalizedRoles.Contains("general"))
        {
            const string upsertGeneralSql = """
                INSERT INTO general (email_general, fecha_registro, estado_verificacion)
                VALUES (@email, CURRENT_TIMESTAMP, @estado_verificacion)
                ON CONFLICT (email_general) DO UPDATE
                SET estado_verificacion = CASE
                    WHEN @actualizar_estado_verificacion THEN EXCLUDED.estado_verificacion
                    ELSE general.estado_verificacion
                END;
                """;

            await using var command = new NpgsqlCommand(upsertGeneralSql, connection, transaction);
            command.Parameters.AddWithValue("email", normalizedEmail);
            command.Parameters.AddWithValue("estado_verificacion", estadoVerificacion ?? false);
            command.Parameters.AddWithValue("actualizar_estado_verificacion", estadoVerificacion.HasValue);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        else
        {
            await DeleteRoleAsync(connection, transaction, "general", "email_general", normalizedEmail, cancellationToken);
        }
    }

    private static async Task DeleteRoleAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string tableName,
        string emailColumn,
        string email,
        CancellationToken cancellationToken)
    {
        var sql = $"DELETE FROM {tableName} WHERE LOWER({emailColumn}) = LOWER(@email);";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("email", email);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ReemplazarTelefonosAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string email,
        IEnumerable<string> telefonos,
        CancellationToken cancellationToken)
    {
        const string deleteSql = """
            DELETE FROM telefonos
            WHERE LOWER(email_usuario) = LOWER(@email);
            """;

        await using (var command = new NpgsqlCommand(deleteSql, connection, transaction))
        {
            command.Parameters.AddWithValue("email", email.Trim().ToLowerInvariant());
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        const string insertSql = """
            INSERT INTO telefonos (email_usuario, telefono)
            VALUES (@email, @telefono);
            """;

        foreach (var telefono in telefonos.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct())
        {
            await using var command = new NpgsqlCommand(insertSql, connection, transaction);
            command.Parameters.AddWithValue("email", email.Trim().ToLowerInvariant());
            command.Parameters.AddWithValue("telefono", telefono);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task<bool> ExisteUsuarioAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string email,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM usuario
                WHERE LOWER(email) = LOWER(@email)
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("email", email.Trim().ToLowerInvariant());

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool exists && exists;
    }

    private static async Task<UsuarioResponse?> GetByEmailUsingConnectionAsync(
        NpgsqlConnection connection,
        string email,
        NpgsqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        var sql = BaseUsuarioSql + "\n" + """
            WHERE LOWER(u.email) = LOWER(@email);
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("email", email.Trim().ToLowerInvariant());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return MapUsuario(reader);
    }

    private static void AddUsuarioParameters(NpgsqlCommand command, CrearUsuarioAdminRequest request)
    {
        command.Parameters.AddWithValue("email", request.Email.Trim().ToLowerInvariant());
        command.Parameters.AddWithValue("nombre", request.Nombre.Trim());
        command.Parameters.AddWithValue("habilitado", request.Habilitado);
        command.Parameters.AddWithValue("pais_documento", request.PaisDocumento.Trim());
        command.Parameters.AddWithValue("tipo_documento", request.TipoDocumento.Trim());
        command.Parameters.AddWithValue("numero_documento", request.NumeroDocumento);
        command.Parameters.AddWithValue("localidad_direccion", (object?)request.LocalidadDireccion?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("calle_direccion", (object?)request.CalleDireccion?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("pais_direccion", (object?)request.PaisDireccion?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("numero_direccion", (object?)request.NumeroDireccion ?? DBNull.Value);
        command.Parameters.AddWithValue("codigo_postal_direccion", (object?)request.CodigoPostalDireccion ?? DBNull.Value);
    }

    private const string BaseUsuarioSql = """
        SELECT
            u.email,
            u.nombre,
            u.habilitado,
            u.pais_documento,
            u.tipo_documento,
            u.numero_documento,
            g.fecha_registro,
            g.estado_verificacion,
            a.pais::text AS pais_admin,
            f.numero_legajo,
            g.email_general IS NOT NULL AS es_general,
            a.email_admin IS NOT NULL AS es_admin,
            f.email_funcionario IS NOT NULL AS es_funcionario
        FROM usuario u
        LEFT JOIN general g ON g.email_general = u.email
        LEFT JOIN admin a ON a.email_admin = u.email
        LEFT JOIN funcionario f ON f.email_funcionario = u.email
        """;

    private static UsuarioResponse MapUsuario(NpgsqlDataReader reader)
    {
        var roles = new List<string>();

        if (reader.GetBoolean(reader.GetOrdinal("es_general"))) roles.Add("General");
        if (reader.GetBoolean(reader.GetOrdinal("es_admin"))) roles.Add("Admin");
        if (reader.GetBoolean(reader.GetOrdinal("es_funcionario"))) roles.Add("Funcionario");

        return new UsuarioResponse
        {
            Email = reader.GetString(reader.GetOrdinal("email")),
            Nombre = reader.GetString(reader.GetOrdinal("nombre")),
            Habilitado = reader.GetBoolean(reader.GetOrdinal("habilitado")),
            PaisDocumento = reader.GetString(reader.GetOrdinal("pais_documento")),
            TipoDocumento = reader.GetString(reader.GetOrdinal("tipo_documento")),
            NumeroDocumento = reader.GetInt32(reader.GetOrdinal("numero_documento")),
            Roles = roles,
            PaisAdmin = reader.IsDBNull(reader.GetOrdinal("pais_admin"))
                ? null
                : reader.GetString(reader.GetOrdinal("pais_admin")),
            NumeroLegajo = reader.IsDBNull(reader.GetOrdinal("numero_legajo"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("numero_legajo")),
            FechaRegistroGeneral = reader.IsDBNull(reader.GetOrdinal("fecha_registro"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("fecha_registro")),
            EstadoVerificacionGeneral = reader.IsDBNull(reader.GetOrdinal("estado_verificacion"))
                ? null
                : reader.GetBoolean(reader.GetOrdinal("estado_verificacion"))
        };
    }
}
