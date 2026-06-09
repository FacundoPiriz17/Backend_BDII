using Backend_BDII.Modules.Auth.DTOs;
using Backend_BDII.Modules.Auth.Models;

namespace Backend_BDII.Modules.Auth.Repositories;

public interface IAuthRepository
{
    Task<AuthUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task RegisterGeneralAsync(RegisterRequest request, string passwordHash, CancellationToken cancellationToken = default);
}