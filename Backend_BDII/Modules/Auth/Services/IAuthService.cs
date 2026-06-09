using Backend_BDII.Modules.Auth.DTOs;

namespace Backend_BDII.Modules.Auth.Services;

public interface IAuthService
{
    Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}