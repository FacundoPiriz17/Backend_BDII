using Backend_BDII.Modules.Entradas.DTOs;
using Backend_BDII.Modules.Entradas.Models;

namespace Backend_BDII.Modules.Entradas.Repositories;

public interface IEntradaRepository
{
    Task<EntradasResponse> CrearEntradaAsync(
        int idCompra,
        IReadOnlyCollection<NuevaEntrada> entrada,
        CancellationToken cancellationToken = default);

    Task<List<EntradasResponse>> GetByUsuarioAsync(string emailUsuario, CancellationToken cancellationToken = default);
    
    Task<EntradasResponse?> GetByIdAsync(int idEntrada, string emailUsuario, CancellationToken cancellationToken = default);
    
    Task<EntradasResponse> ActualizarEstadoAsync(int idEntrada, string emailUsuario, string nuevoEstado, CancellationToken cancellationToken = default);
    
    Task<EntradasResponse> GetEstadioAsync(int idEntrada, int idEstadio, CancellationToken cancellationToken = default);
    
    Task<EntradasResponse> GetSectorEstadioAsync(int idEntrada, int idEstadio, string nombreSector, CancellationToken cancellationToken = default);
    
    Task<EntradasResponse> GetCostoEntradaAsync(int idEntrada,int costoTotal, int idEstadio, string nombreSector, CancellationToken cancellationToken = default);
    
    Task<EntradasResponse?> GetTransferenciasRestantesAsync(int idEntrada, int transferenciasRestantes, CancellationToken cancellationToken = default);
    
    Task<string?> GetQrEntradaAsync(int idEntrada, string codigoQr, CancellationToken cancellationToken = default);
}