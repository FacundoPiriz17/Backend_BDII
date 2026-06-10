using Backend_BDII.Modules.Transferencias.DTOs;

namespace Backend_BDII.Modules.Transferencias.Repositories;

public interface ITransferenciaRepository
{
    Task<TransferenciaResponse> CrearAsync(
        string emailOrigen,
        string emailDestino,
        int idEntrada,
        CancellationToken cancellationToken = default);

    Task<List<TransferenciaResponse>> GetByUsuarioAsync(
        string emailUsuario,
        string relacion,
        string? estado,
        int? idEntrada,
        string? busqueda,
        CancellationToken cancellationToken = default);

    Task<TransferenciaResponse?> GetByIdAsync(int idTransferencia, string emailUsuario, CancellationToken cancellationToken = default);
    Task<TransferenciaResponse?> ActualizarEstadoAsync(int idTransferencia, string emailUsuario, string rolUsuario, string nuevoEstado, CancellationToken cancellationToken = default);
    Task<bool> TieneTransferenciaPendienteAsync(int idEntrada, CancellationToken cancellationToken = default);
}