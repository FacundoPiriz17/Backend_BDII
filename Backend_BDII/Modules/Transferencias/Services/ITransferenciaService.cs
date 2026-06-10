using Backend_BDII.Modules.Transferencias.DTOs;

namespace Backend_BDII.Modules.Transferencias.Services;

public interface ITransferenciaService
{
    Task<TransferenciaResponse> CrearAsync(string emailOrigen, CrearTransferenciaRequest request, CancellationToken cancellationToken = default);
    Task<List<TransferenciaResponse>> GetMisTransferenciasAsync(string emailUsuario, string? relacion, string? estado, int? idEntrada, string? busqueda, CancellationToken cancellationToken = default);
    Task<TransferenciaResponse?> GetByIdAsync(int idTransferencia, string emailUsuario, CancellationToken cancellationToken = default);
    Task<TransferenciaResponse> AceptarAsync(int idTransferencia, string emailUsuario, CancellationToken cancellationToken = default);
    Task<TransferenciaResponse> RechazarAsync(int idTransferencia, string emailUsuario, CancellationToken cancellationToken = default);
    Task<TransferenciaResponse> CancelarAsync(int idTransferencia, string emailUsuario, CancellationToken cancellationToken = default);
}