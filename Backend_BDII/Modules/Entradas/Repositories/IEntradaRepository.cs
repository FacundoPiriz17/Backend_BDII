using Backend_BDII.Modules.Entradas.DTOs;

namespace Backend_BDII.Modules.Entradas.Repositories;

public interface IEntradaRepository
{
    Task<EntradaDetalleResponse?> GetByIdAsync(int idEntrada, string emailUsuario, bool puedeVerTodas, CancellationToken cancellationToken = default);
    Task<CustodiaEntradaResponse?> GetCustodiaAsync(int idEntrada, string emailUsuario, bool puedeVerTodas, CancellationToken cancellationToken = default);
}
