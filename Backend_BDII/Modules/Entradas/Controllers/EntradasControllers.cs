using System.Security.Claims;
using Backend_BDII.Modules.Entradas.DTOs;
using Backend_BDII.Modules.Entradas.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Backend_BDII.Modules.Entradas.Controllers;

[ApiController]
[Route("api/entradas")]
[Authorize(Roles = "General")]
public sealed class EntradasController : ControllerBase
{
    private readonly IEntradaService _entradaService;
    private readonly ILogger<EntradasController> _logger;

    public EntradasController(
        IEntradaService entradaService,
        ILogger<EntradasController> logger)
    {
        _entradaService = entradaService;
        _logger = logger;
    }

    [HttpGet("{emailPropietarioActual}")]
    public async Task<ActionResult<List<EntradasResponse>>> GetEntradasPorUsuario(
        string emailPropietarioActual,
        CancellationToken cancellationToken)
    {
        var entradas = await _entradaService.GetMisEntradasAsync(
            emailPropietarioActual,
            cancellationToken);

        return Ok(entradas);
    }

    [HttpGet("{idEntrada:int}")]
    public async Task<ActionResult<EntradasResponse>> GetById(
        int idEntrada,
        CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return Unauthorized(new { error = "No se pudo obtener el email del token." });

        var entrada = await _entradaService.GetByIdAsync(
            idEntrada,
            email,
            cancellationToken);

        if (entrada is null)
            return NotFound(new { error = "Entrada no encontrada." });

        return Ok(entrada);
    }

    [HttpPut("{idEntrada:int}/estado")]
    public async Task<ActionResult<EntradasResponse>> ActualizarEstado(
        int idEntrada,
        [FromBody] ActualizarEstadoEntradaRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var entrada = await _entradaService.ActualizarEstadoAsync(
                idEntrada,
                request.NuevoEstado,
                cancellationToken);

            return Ok(entrada);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (PostgresException ex)
        {
            _logger.LogWarning(
                ex,
                "Error de PostgreSQL al actualizar estado de entrada.");

            return BadRequest(new { error = ex.MessageText });
        }
    }

    private string? GetEmailFromToken()
    {
        return User.FindFirstValue(ClaimTypes.Email);
    }
}