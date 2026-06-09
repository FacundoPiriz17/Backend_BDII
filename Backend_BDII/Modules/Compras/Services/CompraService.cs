using Backend_BDII.Common.Security;
using Backend_BDII.Modules.Compras.DTOs;
using Backend_BDII.Modules.Compras.Models;
using Backend_BDII.Modules.Compras.Repositories;

namespace Backend_BDII.Modules.Compras.Services;

public sealed class CompraService : ICompraService
{
    private static readonly HashSet<string> SectoresValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "A",
        "B",
        "C",
        "D"
    };

    private readonly ICompraRepository _compraRepository;
    private readonly IEntradaQrCodeService _qrCodeService;

    public CompraService(ICompraRepository compraRepository, IEntradaQrCodeService qrCodeService)
    {
        _compraRepository = compraRepository;
        _qrCodeService = qrCodeService;
    }

    public async Task<CompraResponse> CrearAsync(
        string emailUsuario,
        CrearCompraRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(emailUsuario);
        var entradas = ExpandirEntradas(request, email);

        if (entradas.Count == 0)
            throw new InvalidOperationException("Debe incluir al menos una entrada.");

        if (entradas.Count > 5)
            throw new InvalidOperationException("No se pueden comprar mas de 5 entradas en la misma transaccion.");

        return await _compraRepository.CrearAsync(email, entradas, cancellationToken);
    }

    public Task<List<CompraResponse>> GetMisComprasAsync(string emailUsuario, CancellationToken cancellationToken = default)
    {
        return _compraRepository.GetByUsuarioAsync(NormalizeEmail(emailUsuario), cancellationToken);
    }

    public Task<CompraResponse?> GetByIdAsync(int idCompra, string emailUsuario, CancellationToken cancellationToken = default)
    {
        return _compraRepository.GetByIdAsync(idCompra, NormalizeEmail(emailUsuario), cancellationToken);
    }

    public Task<List<EntradaResponse>> GetMisEntradasAsync(string emailUsuario, CancellationToken cancellationToken = default)
    {
        return _compraRepository.GetEntradasAsignadasAsync(NormalizeEmail(emailUsuario), cancellationToken);
    }

    public Task<List<PartidoDisponibleResponse>> GetPartidosDisponiblesAsync(CancellationToken cancellationToken = default)
    {
        return _compraRepository.GetPartidosDisponiblesAsync(cancellationToken);
    }

    public async Task<CompraResponse> ConfirmarAsync(int idCompra, string emailUsuario, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(emailUsuario);
        var compra = await GetCompraExistenteAsync(idCompra, email, cancellationToken);

        if (compra.Estado != "pendiente")
            throw new InvalidOperationException("Solo se pueden confirmar compras pendientes.");

        return await _compraRepository.ActualizarEstadoAsync(idCompra, email, "confirmada", cancellationToken)
               ?? throw new KeyNotFoundException("Compra no encontrada.");
    }

    public async Task<CompraResponse> PagarAsync(int idCompra, string emailUsuario, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(emailUsuario);
        var compra = await GetCompraExistenteAsync(idCompra, email, cancellationToken);

        if (compra.Estado is not ("pendiente" or "confirmada"))
            throw new InvalidOperationException("Solo se pueden pagar compras pendientes o confirmadas.");

        return await _compraRepository.ActualizarEstadoAsync(idCompra, email, "paga", cancellationToken)
               ?? throw new KeyNotFoundException("Compra no encontrada.");
    }

    public async Task<CompraResponse> CancelarAsync(int idCompra, string emailUsuario, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(emailUsuario);
        var compra = await GetCompraExistenteAsync(idCompra, email, cancellationToken);

        if (compra.Estado == "paga")
            throw new InvalidOperationException("No se puede cancelar una compra paga.");

        if (compra.Estado == "cancelada")
            throw new InvalidOperationException("La compra ya esta cancelada.");

        return await _compraRepository.CancelarAsync(idCompra, email, cancellationToken)
               ?? throw new KeyNotFoundException("Compra no encontrada.");
    }

    public async Task<QrEntradaResponse> RegenerarQrAsync(
        int idEntrada,
        string emailUsuario,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(emailUsuario);
        var payload = _qrCodeService.GeneratePayload(idEntrada, email);
        var codigoQr = await _compraRepository.ActualizarQrEntradaAsync(idEntrada, email, payload, cancellationToken);

        if (codigoQr is null)
            throw new InvalidOperationException("La entrada no existe, no esta activa, no pertenece al usuario o la compra no esta paga.");

        return new QrEntradaResponse
        {
            IdEntrada = idEntrada,
            CodigoQr = codigoQr,
            QrPngBase64 = _qrCodeService.GeneratePngBase64(codigoQr),
            FechaHoraGeneracion = DateTime.UtcNow
        };
    }

    private async Task<CompraResponse> GetCompraExistenteAsync(
        int idCompra,
        string emailUsuario,
        CancellationToken cancellationToken)
    {
        return await _compraRepository.GetByIdAsync(idCompra, emailUsuario, cancellationToken)
               ?? throw new KeyNotFoundException("Compra no encontrada.");
    }

    private List<NuevaEntradaCompra> ExpandirEntradas(CrearCompraRequest request, string emailUsuario)
    {
        var entradas = new List<NuevaEntradaCompra>();

        if (request.Entradas is null)
            return entradas;

        foreach (var entrada in request.Entradas)
        {
            if (entrada.IdPartido <= 0)
                throw new InvalidOperationException("El partido es obligatorio.");

            if (entrada.Cantidad is < 1 or > 5)
                throw new InvalidOperationException("La cantidad debe estar entre 1 y 5.");

            if (string.IsNullOrWhiteSpace(entrada.NombreSector))
                throw new InvalidOperationException("El sector es obligatorio.");

            var nombreSector = entrada.NombreSector.Trim().ToUpperInvariant();

            if (!SectoresValidos.Contains(nombreSector))
                throw new InvalidOperationException("El sector debe ser A, B, C o D.");

            for (var i = 0; i < entrada.Cantidad; i++)
            {
                entradas.Add(new NuevaEntradaCompra
                {
                    IdPartido = entrada.IdPartido,
                    NombreSector = nombreSector,
                    CodigoQr = _qrCodeService.GeneratePayload(null, emailUsuario)
                });
            }
        }

        return entradas;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
