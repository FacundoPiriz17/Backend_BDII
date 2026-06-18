using Backend_BDII.Common.Database;
using Backend_BDII.Modules.Auth.DTOs;
using Npgsql;

namespace Backend_BDII.Modules.Auth.Repositories;

public sealed class AuthRepository : IAuthRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AuthRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> ActualizarContrasenaAsync(string email, string passwordHash, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = "UPDATE login SET contrasena = @hash WHERE LOWER(email) = LOWER(@email);";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("hash", passwordHash);
        command.Parameters.AddWithValue("email", email.Trim().ToLowerInvariant());

        var filas = await command.ExecuteNonQueryAsync(cancellationToken);
        return filas > 0;
    }

    public async Task<AuthUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                u.email,
                u.nombre,
                u.habilitado,
                l.contrasena,
                EXISTS (SELECT 1 FROM general g WHERE g.email_general = u.email) AS es_general,
                EXISTS (SELECT 1 FROM admin a WHERE a.email_admin = u.email) AS es_admin,
                EXISTS (SELECT 1 FROM funcionario f WHERE f.email_funcionario = u.email) AS es_funcionario
            FROM usuario u
            INNER JOIN login l ON l.email = u.email
            WHERE LOWER(u.email) = LOWER(@email);
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email", email);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new AuthUser
        {
            Email = reader.GetString(reader.GetOrdinal("email")),
            Nombre = reader.GetString(reader.GetOrdinal("nombre")),
            Habilitado = reader.GetBoolean(reader.GetOrdinal("habilitado")),
            PasswordHash = reader.GetString(reader.GetOrdinal("contrasena")),
            EsGeneral = reader.GetBoolean(reader.GetOrdinal("es_general")),
            EsAdmin = reader.GetBoolean(reader.GetOrdinal("es_admin")),
            EsFuncionario = reader.GetBoolean(reader.GetOrdinal("es_funcionario"))
        };
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM usuario
                WHERE LOWER(email) = LOWER(@email)
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email", email);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool exists && exists;
    }

    public async Task RegisterGeneralAsync(RegisterRequest request, string passwordHash, CancellationToken cancellationToken = default)
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
                    TRUE,
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
                command.Parameters.AddWithValue("email", request.Email);
                command.Parameters.AddWithValue("nombre", request.Nombre);
                command.Parameters.AddWithValue("pais_documento", request.PaisDocumento);
                command.Parameters.AddWithValue("tipo_documento", request.TipoDocumento);
                command.Parameters.AddWithValue("numero_documento", request.NumeroDocumento);
                command.Parameters.AddWithValue("localidad_direccion", (object?)request.LocalidadDireccion ?? DBNull.Value);
                command.Parameters.AddWithValue("calle_direccion", (object?)request.CalleDireccion ?? DBNull.Value);
                command.Parameters.AddWithValue("pais_direccion", (object?)request.PaisDireccion ?? DBNull.Value);
                command.Parameters.AddWithValue("numero_direccion", (object?)request.NumeroDireccion ?? DBNull.Value);
                command.Parameters.AddWithValue("codigo_postal_direccion", (object?)request.CodigoPostalDireccion ?? DBNull.Value);

                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            const string insertLoginSql = """
                INSERT INTO login (email, contrasena)
                VALUES (@email, @contrasena);
                """;

            await using (var command = new NpgsqlCommand(insertLoginSql, connection, transaction))
            {
                command.Parameters.AddWithValue("email", request.Email);
                command.Parameters.AddWithValue("contrasena", passwordHash);

                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            const string insertGeneralSql = """
                INSERT INTO general (
                    email_general,
                    fecha_registro,
                    estado_verificacion
                )
                VALUES (
                    @email,
                    CURRENT_TIMESTAMP,
                    FALSE
                );
                """;

            await using (var command = new NpgsqlCommand(insertGeneralSql, connection, transaction))
            {
                command.Parameters.AddWithValue("email", request.Email);

                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            const string insertTelefonoSql = """
                INSERT INTO telefonos (email_usuario, telefono)
                VALUES (@email, @telefono);
                """;

            foreach (var telefono in request.Telefonos.Distinct())
            {
                await using var command = new NpgsqlCommand(insertTelefonoSql, connection, transaction);
                command.Parameters.AddWithValue("email", request.Email);
                command.Parameters.AddWithValue("telefono", telefono);

                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}