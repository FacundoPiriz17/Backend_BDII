using Backend_BDII.Common.Security;
using Backend_BDII.Modules.Auth.DTOs;
using Backend_BDII.Modules.Auth.Repositories;

namespace Backend_BDII.Modules.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        IAuthRepository authRepository,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher)
    {
        _authRepository = authRepository;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
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

        return new AuthResponse
        {
            Token = token,
            Email = user.Email,
            Nombre = user.Nombre,
            Roles = roles
        };
    }
}