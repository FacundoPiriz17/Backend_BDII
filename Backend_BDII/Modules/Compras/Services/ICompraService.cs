using Backend_BDII.Modules.Compras.DTOs;

namespace Backend_BDII.Modules.Compras.Services;

public interface ICompraService
{
    Task<CompraResponse> CrearAsync(string emailUsuario, CrearCompraRequest request, CancellationToken cancellationToken = default);
    Task<List<CompraResponse>> GetMisComprasAsync(string emailUsuario, string? estado, int? idPartido, CancellationToken cancellationToken = default);
    Task<CompraResponse?> GetByIdAsync(int idCompra, string emailUsuario, CancellationToken cancellationToken = default);
    Task<List<EntradaResponse>> GetMisEntradasAsync(string emailUsuario, string? estado, int? idPartido, string? busqueda, CancellationToken cancellationToken = default);
    Task<List<PartidoDisponibleResponse>> GetPartidosDisponiblesAsync(CancellationToken cancellationToken = default);
    Task<CompraResponse> ConfirmarAsync(int idCompra, string emailUsuario, CancellationToken cancellationToken = default);
    Task<CompraResponse> PagarAsync(int idCompra, string emailUsuario, CancellationToken cancellationToken = default);
    Task<CompraResponse> CancelarAsync(int idCompra, string emailUsuario, CancellationToken cancellationToken = default);
    Task<QrEntradaResponse> RegenerarQrAsync(int idEntrada, string emailUsuario, CancellationToken cancellationToken = default);
}