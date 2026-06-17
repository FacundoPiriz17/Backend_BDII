using Backend_BDII.Modules.Home.DTOs;

namespace Backend_BDII.Modules.Home.Services;

public interface IHomeService
{
    Task<GeneralHomeResponse> GetGeneralAsync(string email, CancellationToken cancellationToken = default);
    Task<AdminHomeResponse> GetAdminAsync(string email, CancellationToken cancellationToken = default);
    Task<FuncionarioHomeResponse> GetFuncionarioAsync(string email, CancellationToken cancellationToken = default);
}
