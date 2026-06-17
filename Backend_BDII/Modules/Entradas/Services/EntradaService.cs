using Backend_BDII.Modules.Entradas.DTOs;
using Backend_BDII.Modules.Entradas.Repositories;

namespace Backend_BDII.Modules.Entradas.Services;

public sealed class EntradaService : IEntradaService
{
    private readonly IEntradaRepository _entradaRepository;

    public EntradaService(IEntradaRepository entradaRepository)
    {
        _entradaRepository = entradaRepository;
    }

    public Task<EntradaDetalleResponse?> GetByIdAsync(
        int idEntrada,
        string emailUsuario,
        bool puedeVerTodas,
        CancellationToken cancellationToken = default)
    {
        return _entradaRepository.GetByIdAsync(idEntrada, NormalizeEmail(emailUsuario), puedeVerTodas, cancellationToken);
    }

    public Task<CustodiaEntradaResponse?> GetCustodiaAsync(
        int idEntrada,
        string emailUsuario,
        bool puedeVerTodas,
        CancellationToken cancellationToken = default)
    {
        return _entradaRepository.GetCustodiaAsync(idEntrada, NormalizeEmail(emailUsuario), puedeVerTodas, cancellationToken);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
