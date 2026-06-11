using Backend_BDII.Modules.Entradas.DTOs;

namespace Backend_BDII.Modules.Entradas.Services;

public interface IEntradaService
{
    Task<List<EntradasResponse>> GetMisEntradasAsync(string emailUsuario, CancellationToken cancellationToken = default);

    Task<EntradasResponse?> GetByIdAsync(int idEntrada, string emailUsuario, CancellationToken cancellationToken = default);
    
    Task<EntradasResponse> ActualizarEstadoAsync(int idEntrada, string nuevoEstado, CancellationToken cancellationToken = default);
}