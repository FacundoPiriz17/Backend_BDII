using Backend_BDII.Modules.Home.DTOs;
using Backend_BDII.Modules.Home.Repositories;

namespace Backend_BDII.Modules.Home.Services;

public sealed class HomeService : IHomeService
{
    private readonly IHomeRepository _homeRepository;

    public HomeService(IHomeRepository homeRepository)
    {
        _homeRepository = homeRepository;
    }

    public Task<GeneralHomeResponse> GetGeneralAsync(string email, CancellationToken cancellationToken = default)
    {
        return _homeRepository.GetGeneralAsync(NormalizeEmail(email), cancellationToken);
    }

    public Task<AdminHomeResponse> GetAdminAsync(string email, CancellationToken cancellationToken = default)
    {
        return _homeRepository.GetAdminAsync(NormalizeEmail(email), cancellationToken);
    }

    public Task<FuncionarioHomeResponse> GetFuncionarioAsync(string email, CancellationToken cancellationToken = default)
    {
        return _homeRepository.GetFuncionarioAsync(NormalizeEmail(email), cancellationToken);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
