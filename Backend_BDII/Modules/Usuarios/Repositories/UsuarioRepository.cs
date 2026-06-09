using Backend_BDII.Common.Database;
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

    public async Task<List<UsuarioResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                u.email,
                u.nombre,
                u.habilitado,
                u.pais_documento,
                u.tipo_documento,
                u.numero_documento,
                EXISTS (SELECT 1 FROM general g WHERE g.email_general = u.email) AS es_general,
                EXISTS (SELECT 1 FROM admin a WHERE a.email_admin = u.email) AS es_admin,
                EXISTS (SELECT 1 FROM funcionario f WHERE f.email_funcionario = u.email) AS es_funcionario
            FROM usuario u
            ORDER BY u.email;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
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

        const string sql = """
            SELECT
                u.email,
                u.nombre,
                u.habilitado,
                u.pais_documento,
                u.tipo_documento,
                u.numero_documento,
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

        return MapUsuario(reader);
    }

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
            Roles = roles
        };
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
}