using System.Security.Claims;
using Backend_BDII.Common.Responses;
using Backend_BDII.Modules.Compras.DTOs;
using Backend_BDII.Modules.Compras.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Backend_BDII.Modules.Compras.Controllers;

[ApiController]
[Route("api/compras")]
[Authorize(Roles = "General")]
public sealed class ComprasController : ControllerBase
{
    private readonly ICompraService _compraService;
    private readonly ILogger<ComprasController> _logger;

    public ComprasController(ICompraService compraService, ILogger<ComprasController> logger)
    {
        _compraService = compraService;
        _logger = logger;
    }

    [HttpGet("partidos-disponibles")]
    public async Task<ActionResult<List<PartidoDisponibleResponse>>> GetPartidosDisponibles(CancellationToken cancellationToken)
    {
        var partidos = await _compraService.GetPartidosDisponiblesAsync(cancellationToken);
        return Ok(partidos);
    }

    [HttpPost]
    public async Task<ActionResult<CompraResponse>> Crear([FromBody] CrearCompraRequest request, CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var compra = await _compraService.CrearAsync(email, request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { idCompra = compra.IdCompra }, compra);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
        catch (PostgresException ex)
        {
            _logger.LogWarning(ex, "Error de PostgreSQL al crear compra.");
            return this.BadRequestError(ex.MessageText, "database_error");
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<CompraResponse>>> GetMisCompras(
        [FromQuery] string? estado,
        [FromQuery] int? idPartido,
        CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var compras = await _compraService.GetMisComprasAsync(email, estado, idPartido, cancellationToken);
            return Ok(compras);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
    }

    [HttpGet("{idCompra:int}")]
    public async Task<ActionResult<CompraResponse>> GetById(int idCompra, CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        var compra = await _compraService.GetByIdAsync(idCompra, email, cancellationToken);

        if (compra is null)
            return this.NotFoundError("Compra no encontrada.");

        return Ok(compra);
    }

    [HttpGet("mis-entradas")]
    public async Task<ActionResult<List<EntradaResponse>>> GetMisEntradas(
        [FromQuery] string? estado,
        [FromQuery] int? idPartido,
        [FromQuery] string? busqueda,
        CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var entradas = await _compraService.GetMisEntradasAsync(email, estado, idPartido, busqueda, cancellationToken);
            return Ok(entradas);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
    }

    [HttpPost("{idCompra:int}/confirmar")]
    public Task<ActionResult<CompraResponse>> Confirmar(int idCompra, CancellationToken cancellationToken)
    {
        return CambiarEstadoCompra(idCompra, "confirmar", cancellationToken);
    }

    [HttpPost("{idCompra:int}/pagar")]
    public Task<ActionResult<CompraResponse>> Pagar(int idCompra, CancellationToken cancellationToken)
    {
        return CambiarEstadoCompra(idCompra, "pagar", cancellationToken);
    }

    [HttpPost("{idCompra:int}/cancelar")]
    public Task<ActionResult<CompraResponse>> Cancelar(int idCompra, CancellationToken cancellationToken)
    {
        return CambiarEstadoCompra(idCompra, "cancelar", cancellationToken);
    }

    [HttpPost("entradas/{idEntrada:int}/qr")]
    public async Task<ActionResult<QrEntradaResponse>> RegenerarQr(int idEntrada, CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var qr = await _compraService.RegenerarQrAsync(idEntrada, email, cancellationToken);
            return Ok(qr);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
    }

    private async Task<ActionResult<CompraResponse>> CambiarEstadoCompra(
        int idCompra,
        string accion,
        CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var compra = accion switch
            {
                "confirmar" => await _compraService.ConfirmarAsync(idCompra, email, cancellationToken),
                "pagar" => await _compraService.PagarAsync(idCompra, email, cancellationToken),
                "cancelar" => await _compraService.CancelarAsync(idCompra, email, cancellationToken),
                _ => throw new InvalidOperationException("Accion de compra invalida.")
            };

            return Ok(compra);
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFoundError(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
        catch (PostgresException ex)
        {
            _logger.LogWarning(ex, "Error de PostgreSQL al cambiar estado de compra.");
            return this.BadRequestError(ex.MessageText, "database_error");
        }
    }

    private string? GetEmailFromToken()
    {
        return User.FindFirstValue(ClaimTypes.Email);
    }
}
