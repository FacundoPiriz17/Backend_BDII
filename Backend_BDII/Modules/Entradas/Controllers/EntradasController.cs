using System.Security.Claims;
using Backend_BDII.Common.Responses;
using Backend_BDII.Modules.Compras.Services;
using Backend_BDII.Modules.Entradas.DTOs;
using Backend_BDII.Modules.Entradas.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_BDII.Modules.Entradas.Controllers;

[ApiController]
[Route("api/entradas")]
[Authorize(Roles = "General,Admin,Funcionario")]
public sealed class EntradasController : ControllerBase
{
    private readonly IEntradaService _entradaService;
    private readonly ICompraService _compraService;

    public EntradasController(IEntradaService entradaService, ICompraService compraService)
    {
        _entradaService = entradaService;
        _compraService = compraService;
    }

    [HttpGet("{idEntrada:int}")]
    public async Task<ActionResult<EntradaDetalleResponse>> GetById(int idEntrada, CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        var entrada = await _entradaService.GetByIdAsync(
            idEntrada,
            email,
            PuedeVerTodas(),
            cancellationToken);

        if (entrada is null)
            return this.NotFoundError("Entrada no encontrada.");

        return Ok(entrada);
    }

    [HttpGet("{idEntrada:int}/vista")]
    [Authorize(Roles = "General")]
    public async Task<ActionResult<EntradaVistaResponse>> GetVista(int idEntrada, CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        var entrada = await _entradaService.GetByIdAsync(
            idEntrada,
            email,
            false,
            cancellationToken);

        if (entrada is null)
            return this.NotFoundError("Entrada no encontrada.");

        try
        {
            var qr = await _compraService.RegenerarQrAsync(idEntrada, email, cancellationToken);
            var entradaActualizada = await _entradaService.GetByIdAsync(
                idEntrada,
                email,
                false,
                cancellationToken) ?? entrada;

            return Ok(new EntradaVistaResponse
            {
                Entrada = entradaActualizada,
                Qr = qr
            });
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
    }

    [HttpGet("{idEntrada:int}/custodia")]
    public async Task<ActionResult<CustodiaEntradaResponse>> GetCustodia(int idEntrada, CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        var custodia = await _entradaService.GetCustodiaAsync(
            idEntrada,
            email,
            PuedeVerTodas(),
            cancellationToken);

        if (custodia is null)
            return this.NotFoundError("Entrada no encontrada.");

        return Ok(custodia);
    }

    private bool PuedeVerTodas()
    {
        return User.IsInRole("Admin") || User.IsInRole("Funcionario");
    }

    private string? GetEmailFromToken()
    {
        return User.FindFirstValue(ClaimTypes.Email);
    }
}
