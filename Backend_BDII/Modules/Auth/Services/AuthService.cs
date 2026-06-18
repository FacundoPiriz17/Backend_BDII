using Backend_BDII.Common.Auditing;
using Backend_BDII.Common.Security;
using Backend_BDII.Modules.Auth.DTOs;
using Backend_BDII.Modules.Auth.Repositories;

namespace Backend_BDII.Modules.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _auditService;

    public AuthService(
        IAuthRepository authRepository,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher,
        IAuditService auditService)
    {
        _authRepository = authRepository;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _auditService = auditService;
    }
    
    public async Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new InvalidOperationException("La contraseña debe tener al menos 6 caracteres.");

        if (await _authRepository.ExistsByEmailAsync(email, cancellationToken))
            throw new InvalidOperationException("Ya existe un usuario registrado con ese email.");
        
        var passwordHash = _passwordHasher.Hash(request.Password);
        
        await _authRepository.RegisterGeneralAsync(
            new RegisterRequest
            {
                Email = email,
                Nombre = request.Nombre.Trim(),
                Password = request.Password,
                PaisDocumento = request.PaisDocumento.Trim(),
                TipoDocumento = request.TipoDocumento.Trim(),
                NumeroDocumento = request.NumeroDocumento,
                LocalidadDireccion = request.LocalidadDireccion?.Trim(),
                CalleDireccion = request.CalleDireccion?.Trim(),
                PaisDireccion = request.PaisDireccion?.Trim(),
                NumeroDireccion = request.NumeroDireccion,
                CodigoPostalDireccion = request.CodigoPostalDireccion,
                Telefonos = request.Telefonos
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t.Trim())
                    .Distinct()
                    .ToList()
            },
            passwordHash,
            cancellationToken
        );

        _auditService.Record("auth.register", email, new { Rol = "General" });
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _authRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null)
            throw new UnauthorizedAccessException("Email o contraseña incorrectos.");

        if (!user.Habilitado)
            throw new UnauthorizedAccessException("El usuario está deshabilitado.");

        var passwordOk = _passwordHasher.Verify(request.Password, user.PasswordHash);
        
        if (!passwordOk)
            throw new UnauthorizedAccessException("Email o contraseña incorrectos.");

        var roles = user.GetRoles();

        var token = _jwtTokenService.GenerateToken(new JwtUser
        {
            Email = user.Email,
            Nombre = user.Nombre,
            Roles = roles
        });

        _auditService.Record("auth.login", user.Email, new { Roles = roles });

        return new AuthResponse
        {
            Token = token,
            Email = user.Email,
            Nombre = user.Nombre,
            Roles = roles
        };
    }
    
    public async Task CambiarContrasenaAsync(
        string email,
        string contrasenaActual,
        string contrasenaNueva,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(contrasenaNueva) || contrasenaNueva.Length < 6)
            throw new InvalidOperationException("La nueva contraseña debe tener al menos 6 caracteres.");

        if (contrasenaActual == contrasenaNueva)
            throw new InvalidOperationException("La nueva contraseña debe ser distinta a la actual.");

        var user = await _authRepository.GetByEmailAsync(normalizedEmail, cancellationToken)
            ?? throw new UnauthorizedAccessException("Usuario no encontrado.");

        if (!_passwordHasher.Verify(contrasenaActual, user.PasswordHash))
            throw new UnauthorizedAccessException("La contraseña actual es incorrecta.");

        var hash = _passwordHasher.Hash(contrasenaNueva);
        var ok = await _authRepository.ActualizarContrasenaAsync(normalizedEmail, hash, cancellationToken);

        if (!ok)
            throw new InvalidOperationException("No se pudo actualizar la contraseña.");

        _auditService.Record("auth.cambiar_contrasena", normalizedEmail, null);
    }
    
    public async Task<AuthResponse> RefreshAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _authRepository.GetByEmailAsync(email, cancellationToken)
            ?? throw new UnauthorizedAccessException("La sesión ya no es válida.");

        if (!user.Habilitado)
            throw new UnauthorizedAccessException("El usuario está deshabilitado.");

        var roles = user.GetRoles();

        var token = _jwtTokenService.GenerateToken(new JwtUser
        {
            Email = user.Email,
            Nombre = user.Nombre,
            Roles = roles
        });

        _auditService.Record("auth.refresh", user.Email, new { Roles = roles });

        return new AuthResponse
        {
            Token = token,
            Email = user.Email,
            Nombre = user.Nombre,
            Roles = roles
        };
    }
}
