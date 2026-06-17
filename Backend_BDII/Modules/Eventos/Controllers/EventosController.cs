using System.Security.Claims;
using Backend_BDII.Common.Responses;
using Backend_BDII.Modules.Eventos.DTOs;
using Backend_BDII.Modules.Eventos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Backend_BDII.Modules.Eventos.Controllers;

[ApiController]
[Route("api/eventos")]
[Authorize]
public sealed class EventosController : ControllerBase
{
    private readonly IEventoService _eventoService;
    private readonly ILogger<EventosController> _logger;

    public EventosController(IEventoService eventoService, ILogger<EventosController> logger)
    {
        _eventoService = eventoService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<EventoResponse>>> GetAll(
        [FromQuery] bool soloFuturos = false,
        [FromQuery] string? busqueda = null,
        [FromQuery] string? pais = null,
        [FromQuery] string? equipo = null,
        [FromQuery] string? fase = null,
        [FromQuery] string? estado = null,
        [FromQuery] DateOnly? desde = null,
        [FromQuery] DateOnly? hasta = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var eventos = await _eventoService.GetAllAsync(soloFuturos, busqueda, pais, equipo, fase, estado, desde, hasta, cancellationToken);
            return Ok(eventos);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
    }

    [HttpGet("{idPartido:int}")]
    public async Task<ActionResult<EventoResponse>> GetById(int idPartido, CancellationToken cancellationToken)
    {
        var evento = await _eventoService.GetByIdAsync(idPartido, cancellationToken);

        if (evento is null)
            return this.NotFoundError("Evento no encontrado.");

        return Ok(evento);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EventoResponse>> Crear([FromBody] CrearEventoRequest request, CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var evento = await _eventoService.CrearAsync(email, request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { idPartido = evento.IdPartido }, evento);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
        catch (PostgresException ex)
        {
            _logger.LogWarning(ex, "Error de PostgreSQL al crear evento.");
            return this.BadRequestError(ex.MessageText, "database_error");
        }
    }

    [HttpPut("{idPartido:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EventoResponse>> Actualizar(
        int idPartido,
        [FromBody] ActualizarEventoRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var evento = await _eventoService.ActualizarAsync(idPartido, email, request, cancellationToken);
            return Ok(evento);
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
            _logger.LogWarning(ex, "Error de PostgreSQL al actualizar evento.");
            return this.BadRequestError(ex.MessageText, "database_error");
        }
    }

    [HttpPatch("{idPartido:int}/estado")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EventoResponse>> CambiarEstado(
        int idPartido,
        [FromBody] CambiarEstadoEventoRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var evento = await _eventoService.CambiarEstadoAsync(idPartido, email, request, cancellationToken);
            return Ok(evento);
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
            _logger.LogWarning(ex, "Error de PostgreSQL al cambiar estado de evento.");
            return this.BadRequestError(ex.MessageText, "database_error");
        }
    }

    private string? GetEmailFromToken()
    {
        return User.FindFirstValue(ClaimTypes.Email);
    }
}
