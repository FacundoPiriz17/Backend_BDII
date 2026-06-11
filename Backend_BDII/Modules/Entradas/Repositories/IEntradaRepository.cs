using Backend_BDII.Modules.Entradas.DTOs;
using Backend_BDII.Modules.Entradas.Models;

namespace Backend_BDII.Modules.Entradas.Repositories;

public interface IEntradaRepository
{
    Task<List<EntradasResponse>> GetByUsuarioAsync(string emailUsuario, CancellationToken cancellationToken = default);

    Task<EntradasResponse?> GetByIdAsync(int idEntrada, CancellationToken cancellationToken = default);
    
    Task<EntradasResponse?> ActualizarEstadoAsync(int idEntrada, string nuevoEstado, CancellationToken cancellationToken = default);

}