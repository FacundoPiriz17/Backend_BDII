using System.Security.Claims;
using Backend_BDII.Common.Responses;
using Backend_BDII.Modules.Transferencias.DTOs;
using Backend_BDII.Modules.Transferencias.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Backend_BDII.Modules.Transferencias.Controllers;

[ApiController]
[Route("api/transferencias")]
[Authorize(Roles = "General")]
public sealed class TransferenciasController : ControllerBase
{
    private readonly ITransferenciaService _transferenciaService;
    private readonly ILogger<TransferenciasController> _logger;

    public TransferenciasController(
        ITransferenciaService transferenciaService,
        ILogger<TransferenciasController> logger)
    {
        _transferenciaService = transferenciaService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<TransferenciaResponse>> Crear(
        [FromBody] CrearTransferenciaRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var transferencia = await _transferenciaService.CrearAsync(email, request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { idTransferencia = transferencia.IdTransferencia }, transferencia);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
        catch (PostgresException ex)
        {
            _logger.LogWarning(ex, "Error de PostgreSQL al crear transferencia.");
            return this.BadRequestError(ex.MessageText, "database_error");
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<TransferenciaResponse>>> GetMisTransferencias(
        [FromQuery] string? relacion,
        [FromQuery] string? estado,
        [FromQuery] int? idEntrada,
        [FromQuery] string? busqueda,
        CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var transferencias = await _transferenciaService.GetMisTransferenciasAsync(
                email,
                relacion,
                estado,
                idEntrada,
                busqueda,
                cancellationToken);

            return Ok(transferencias);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
    }

    [HttpGet("{idTransferencia:int}")]
    public async Task<ActionResult<TransferenciaResponse>> GetById(
        int idTransferencia,
        CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        var transferencia = await _transferenciaService.GetByIdAsync(idTransferencia, email, cancellationToken);

        if (transferencia is null)
            return this.NotFoundError("Transferencia no encontrada.");

        return Ok(transferencia);
    }

    [HttpPost("{idTransferencia:int}/aceptar")]
    public Task<ActionResult<TransferenciaResponse>> Aceptar(int idTransferencia, CancellationToken cancellationToken)
    {
        return CambiarEstadoTransferencia(idTransferencia, "aceptar", cancellationToken);
    }

    [HttpPost("{idTransferencia:int}/rechazar")]
    public Task<ActionResult<TransferenciaResponse>> Rechazar(int idTransferencia, CancellationToken cancellationToken)
    {
        return CambiarEstadoTransferencia(idTransferencia, "rechazar", cancellationToken);
    }

    [HttpPost("{idTransferencia:int}/cancelar")]
    public Task<ActionResult<TransferenciaResponse>> Cancelar(int idTransferencia, CancellationToken cancellationToken)
    {
        return CambiarEstadoTransferencia(idTransferencia, "cancelar", cancellationToken);
    }

    private async Task<ActionResult<TransferenciaResponse>> CambiarEstadoTransferencia(
        int idTransferencia,
        string accion,
        CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var transferencia = accion switch
            {
                "aceptar" => await _transferenciaService.AceptarAsync(idTransferencia, email, cancellationToken),
                "rechazar" => await _transferenciaService.RechazarAsync(idTransferencia, email, cancellationToken),
                "cancelar" => await _transferenciaService.CancelarAsync(idTransferencia, email, cancellationToken),
                _ => throw new InvalidOperationException("Accion de transferencia invalida.")
            };

            return Ok(transferencia);
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFoundError(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return this.ForbiddenError(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
        catch (PostgresException ex)
        {
            _logger.LogWarning(ex, "Error de PostgreSQL al cambiar estado de transferencia.");
            return this.BadRequestError(ex.MessageText, "database_error");
        }
    }

    private string? GetEmailFromToken()
    {
        return User.FindFirstValue(ClaimTypes.Email);
    }
}
