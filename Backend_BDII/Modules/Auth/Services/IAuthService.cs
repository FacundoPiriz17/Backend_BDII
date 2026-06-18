using Backend_BDII.Modules.Auth.DTOs;

namespace Backend_BDII.Modules.Auth.Services;

public interface IAuthService
{
    Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshAsync(string email, CancellationToken cancellationToken = default);
    Task CambiarContrasenaAsync(string email, string contrasenaActual, string contrasenaNueva, CancellationToken cancellationToken = default);
}