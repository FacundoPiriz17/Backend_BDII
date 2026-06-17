using Backend_BDII.Modules.Compras.DTOs;

namespace Backend_BDII.Modules.Compras.Repositories;

public interface ICompraRepository
{
    Task<CompraResponse> CrearAsync(
        string emailUsuario,
        IReadOnlyCollection<NuevaEntradaCompra> entradas,
        CancellationToken cancellationToken = default);

    Task<List<CompraResponse>> GetByUsuarioAsync(string emailUsuario, string? estado, int? idPartido, CancellationToken cancellationToken = default);
    Task<CompraResponse?> GetByIdAsync(int idCompra, string emailUsuario, CancellationToken cancellationToken = default);
    Task<List<EntradaResponse>> GetEntradasAsignadasAsync(string emailUsuario, string? estado, int? idPartido, string? busqueda, CancellationToken cancellationToken = default);
    Task<List<PartidoDisponibleResponse>> GetPartidosDisponiblesAsync(CancellationToken cancellationToken = default);
    Task<CompraResponse?> ActualizarEstadoAsync(int idCompra, string emailUsuario, string nuevoEstado, CancellationToken cancellationToken = default);
    Task<CompraResponse?> CancelarAsync(int idCompra, string emailUsuario, CancellationToken cancellationToken = default);
    Task<string?> ActualizarQrEntradaAsync(int idEntrada, string emailUsuario, string codigoQr, CancellationToken cancellationToken = default);
}