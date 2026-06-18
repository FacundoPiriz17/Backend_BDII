using Backend_BDII.Common.Auditing;
using Backend_BDII.Common.Security;
using Backend_BDII.Modules.Usuarios.DTOs;
using Backend_BDII.Modules.Usuarios.Repositories;

namespace Backend_BDII.Modules.Usuarios.Services;

public sealed class UsuarioService : IUsuarioService
{
    private static readonly HashSet<string> RolesValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "General",
        "Admin",
        "Funcionario"
    };

    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _auditService;

    public UsuarioService(
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        IAuditService auditService)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _auditService = auditService;
    }

    public Task<List<UsuarioResponse>> GetAllAsync(
        string? rol,
        bool? habilitado,
        string? busqueda,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(rol) && !RolesValidos.Contains(rol.Trim()))
            throw new InvalidOperationException("El rol debe ser General, Admin o Funcionario.");

        return _usuarioRepository.GetAllAsync(rol, habilitado, busqueda, cancellationToken);
    }

    public Task<UsuarioResponse?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _usuarioRepository.GetByEmailAsync(NormalizeEmail(email), cancellationToken);
    }

    public Task<MiPerfilResponse?> GetMiPerfilAsync(string email, CancellationToken cancellationToken = default)
    {
        return _usuarioRepository.GetMiPerfilAsync(NormalizeEmail(email), cancellationToken);
    }

    public async Task<UsuarioResponse> CrearAsync(
        string emailAdmin,
        CrearUsuarioAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidarUsuarioBase(request.Email, request.Nombre, request.Password, request.PaisDocumento, request.TipoDocumento, request.NumeroDocumento);
        ValidarRoles(request.Roles, request.PaisAdmin, request.NumeroLegajo);

        if (NormalizeRoles(request.Roles).Count > 1)
            throw new InvalidOperationException("Al crear un usuario solo se puede asignar un rol.");

        var normalizedRequest = new CrearUsuarioAdminRequest
        {
            Email = NormalizeEmail(request.Email),
            Nombre = request.Nombre.Trim(),
            Password = request.Password,
            Habilitado = request.Habilitado,
            PaisDocumento = request.PaisDocumento.Trim(),
            TipoDocumento = request.TipoDocumento.Trim(),
            NumeroDocumento = request.NumeroDocumento,
            LocalidadDireccion = request.LocalidadDireccion?.Trim(),
            CalleDireccion = request.CalleDireccion?.Trim(),
            PaisDireccion = request.PaisDireccion?.Trim(),
            NumeroDireccion = request.NumeroDireccion,
            CodigoPostalDireccion = request.CodigoPostalDireccion,
            Telefonos = NormalizeTelefonos(request.Telefonos),
            Roles = NormalizeRoles(request.Roles),
            PaisAdmin = request.PaisAdmin?.Trim(),
            NumeroLegajo = request.NumeroLegajo,
            EstadoVerificacion = request.EstadoVerificacion
        };

        var usuario = await _usuarioRepository.CrearAsync(
            normalizedRequest,
            _passwordHasher.Hash(request.Password),
            cancellationToken);

        _auditService.Record("usuario.crear", emailAdmin, new
        {
            usuario.Email,
            usuario.Roles
        });

        return usuario;
    }

    public async Task<UsuarioResponse> ActualizarAsync(
        string emailAdmin,
        string email,
        ActualizarUsuarioRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidarUsuarioBase(email, request.Nombre, "password-valida", request.PaisDocumento, request.TipoDocumento, request.NumeroDocumento);

        var usuario = await _usuarioRepository.ActualizarAsync(
            NormalizeEmail(email),
            new ActualizarUsuarioRequest
            {
                Nombre = request.Nombre.Trim(),
                Habilitado = request.Habilitado,
                PaisDocumento = request.PaisDocumento.Trim(),
                TipoDocumento = request.TipoDocumento.Trim(),
                NumeroDocumento = request.NumeroDocumento,
                LocalidadDireccion = request.LocalidadDireccion?.Trim(),
                CalleDireccion = request.CalleDireccion?.Trim(),
                PaisDireccion = request.PaisDireccion?.Trim(),
                NumeroDireccion = request.NumeroDireccion,
                CodigoPostalDireccion = request.CodigoPostalDireccion,
                Telefonos = NormalizeTelefonos(request.Telefonos)
            },
            cancellationToken) ?? throw new KeyNotFoundException("Usuario no encontrado.");

        _auditService.Record("usuario.actualizar", emailAdmin, new
        {
            usuario.Email,
            usuario.Habilitado
        });

        return usuario;
    }

    public async Task<MiPerfilResponse> ActualizarMiPerfilAsync(
        string email,
        ActualizarMiPerfilRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);

        if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length < 3)
            throw new InvalidOperationException("El nombre debe tener al menos 3 caracteres.");
        
        var actual = await _usuarioRepository.GetByEmailAsync(normalizedEmail, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        var actualizado = await _usuarioRepository.ActualizarAsync(
            normalizedEmail,
            new ActualizarUsuarioRequest
            {
                Nombre = request.Nombre.Trim(),
                Habilitado = actual.Habilitado,
                PaisDocumento = actual.PaisDocumento,
                TipoDocumento = actual.TipoDocumento,
                NumeroDocumento = actual.NumeroDocumento,
                LocalidadDireccion = request.LocalidadDireccion?.Trim(),
                CalleDireccion = request.CalleDireccion?.Trim(),
                PaisDireccion = request.PaisDireccion?.Trim(),
                NumeroDireccion = request.NumeroDireccion,
                CodigoPostalDireccion = request.CodigoPostalDireccion,
                Telefonos = NormalizeTelefonos(request.Telefonos)
            },
            cancellationToken);

        if (actualizado is null)
            throw new KeyNotFoundException("Usuario no encontrado.");

        _auditService.Record("usuario.perfil.actualizar", normalizedEmail, new
        {
            Email = normalizedEmail
        });

        return await _usuarioRepository.GetMiPerfilAsync(normalizedEmail, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");
    }

    public async Task<UsuarioResponse> ActualizarHabilitacionAsync(
        string emailAdmin,
        string email,
        bool habilitado,
        CancellationToken cancellationToken = default)
    {
        if (NormalizeEmail(emailAdmin) == NormalizeEmail(email))
            throw new InvalidOperationException("No podés cambiar tu propio estado de habilitación.");

        var usuario = await _usuarioRepository.ActualizarHabilitacionAsync(
            NormalizeEmail(email),
            habilitado,
            cancellationToken) ?? throw new KeyNotFoundException("Usuario no encontrado.");

        _auditService.Record("usuario.habilitacion", emailAdmin, new
        {
            usuario.Email,
            usuario.Habilitado
        });

        return usuario;
    }

    public async Task<UsuarioResponse> ActualizarRolesAsync(
        string emailAdmin,
        string email,
        ActualizarRolesUsuarioRequest request,
        CancellationToken cancellationToken = default)
    {
        if (NormalizeEmail(emailAdmin) == NormalizeEmail(email))
            throw new InvalidOperationException("No podés modificar tus propios roles.");

        ValidarRoles(request.Roles, request.PaisAdmin, request.NumeroLegajo);

        var normalizedRequest = new ActualizarRolesUsuarioRequest
        {
            Roles = NormalizeRoles(request.Roles),
            PaisAdmin = request.PaisAdmin?.Trim(),
            NumeroLegajo = request.NumeroLegajo,
            EstadoVerificacion = request.EstadoVerificacion
        };

        var usuario = await _usuarioRepository.ActualizarRolesAsync(
            NormalizeEmail(email),
            normalizedRequest,
            cancellationToken) ?? throw new KeyNotFoundException("Usuario no encontrado.");

        _auditService.Record("usuario.roles", emailAdmin, new
        {
            usuario.Email,
            usuario.Roles
        });

        return usuario;
    }

    private static void ValidarUsuarioBase(
        string email,
        string nombre,
        string password,
        string paisDocumento,
        string tipoDocumento,
        int numeroDocumento)
    {
        if (!EsEmailUcu(email))
            throw new InvalidOperationException("El email debe pertenecer al dominio @ucu.edu.uy o @correo.ucu.edu.uy.");

        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length < 3)
            throw new InvalidOperationException("El nombre debe tener al menos 3 caracteres.");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            throw new InvalidOperationException("La contrasena debe tener al menos 6 caracteres.");

        if (string.IsNullOrWhiteSpace(paisDocumento))
            throw new InvalidOperationException("El pais del documento es obligatorio.");

        if (string.IsNullOrWhiteSpace(tipoDocumento))
            throw new InvalidOperationException("El tipo de documento es obligatorio.");

        if (numeroDocumento <= 0)
            throw new InvalidOperationException("El numero de documento debe ser mayor a 0.");
    }

    private static void ValidarRoles(IEnumerable<string>? roles, string? paisAdmin, int? numeroLegajo)
    {
        var normalizedRoles = NormalizeRoles(roles ?? []);

        if (normalizedRoles.Count == 0)
            throw new InvalidOperationException("Debe indicar al menos un rol.");

        if (normalizedRoles.Any(role => !RolesValidos.Contains(role)))
            throw new InvalidOperationException("Los roles validos son General, Admin y Funcionario.");

        if (normalizedRoles.Contains("Admin") && string.IsNullOrWhiteSpace(paisAdmin))
            throw new InvalidOperationException("El pais sede es obligatorio para el rol Admin.");

        if (normalizedRoles.Contains("Funcionario") && (!numeroLegajo.HasValue || numeroLegajo <= 0))
            throw new InvalidOperationException("El numero de legajo es obligatorio para el rol Funcionario.");
    }

    private static List<string> NormalizeRoles(IEnumerable<string> roles)
    {
        return roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim().ToLowerInvariant() switch
            {
                "general" => "General",
                "admin" => "Admin",
                "funcionario" => "Funcionario",
                var value => value
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> NormalizeTelefonos(IEnumerable<string> telefonos)
    {
        return telefonos
            .Where(telefono => !string.IsNullOrWhiteSpace(telefono))
            .Select(telefono => telefono.Trim())
            .Distinct()
            .ToList();
    }

    private static bool EsEmailUcu(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return normalizedEmail.EndsWith("@ucu.edu.uy")
               || normalizedEmail.EndsWith("@correo.ucu.edu.uy");
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
