using System.Security.Claims;
using Backend_BDII.Common.Responses;
using Backend_BDII.Modules.Infraestructura.DTOs;
using Backend_BDII.Modules.Infraestructura.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Backend_BDII.Modules.Infraestructura.Controllers;

[ApiController]
[Route("api/infraestructura")]
[Authorize]
public sealed class InfraestructuraController : ControllerBase
{
    private readonly IInfraestructuraService _infraestructuraService;
    private readonly ILogger<InfraestructuraController> _logger;

    public InfraestructuraController(
        IInfraestructuraService infraestructuraService,
        ILogger<InfraestructuraController> logger)
    {
        _infraestructuraService = infraestructuraService;
        _logger = logger;
    }

    [HttpGet("estadios")]
    public async Task<ActionResult<List<EstadioResponse>>> GetEstadios([FromQuery] string? pais, CancellationToken cancellationToken)
    {
        var estadios = await _infraestructuraService.GetEstadiosAsync(pais, cancellationToken);
        return Ok(estadios);
    }

    [HttpGet("estadios/{idEstadio:int}")]
    public async Task<ActionResult<EstadioResponse>> GetEstadioById(int idEstadio, CancellationToken cancellationToken)
    {
        var estadio = await _infraestructuraService.GetEstadioByIdAsync(idEstadio, cancellationToken);

        if (estadio is null)
            return this.NotFoundError("Estadio no encontrado.");

        return Ok(estadio);
    }

    [HttpPost("estadios")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EstadioResponse>> CrearEstadio([FromBody] CrearEstadioRequest request, CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var estadio = await _infraestructuraService.CrearEstadioAsync(email, request, cancellationToken);
            return CreatedAtAction(nameof(GetEstadioById), new { idEstadio = estadio.IdEstadio }, estadio);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
        catch (PostgresException ex)
        {
            _logger.LogWarning(ex, "Error de PostgreSQL al crear estadio.");
            return this.BadRequestError(ex.MessageText, "database_error");
        }
    }

    [HttpPut("estadios/{idEstadio:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EstadioResponse>> ActualizarEstadio(
        int idEstadio,
        [FromBody] ActualizarEstadioRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var estadio = await _infraestructuraService.ActualizarEstadioAsync(idEstadio, email, request, cancellationToken);
            return Ok(estadio);
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
            _logger.LogWarning(ex, "Error de PostgreSQL al actualizar estadio.");
            return this.BadRequestError(ex.MessageText, "database_error");
        }
    }

    [HttpPut("estadios/{idEstadio:int}/sectores/{nombreSector}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SectorInfraestructuraResponse>> ActualizarSector(
        int idEstadio,
        string nombreSector,
        [FromBody] ActualizarSectorRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var sector = await _infraestructuraService.ActualizarSectorAsync(idEstadio, nombreSector, email, request, cancellationToken);
            return Ok(sector);
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
            _logger.LogWarning(ex, "Error de PostgreSQL al actualizar sector.");
            return this.BadRequestError(ex.MessageText, "database_error");
        }
    }

    [HttpGet("equipos")]
    public async Task<ActionResult<List<EquipoResponse>>> GetEquipos(CancellationToken cancellationToken)
    {
        var equipos = await _infraestructuraService.GetEquiposAsync(cancellationToken);
        return Ok(equipos);
    }

    [HttpGet("dispositivos")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<DispositivoResponse>>> GetDispositivos(
        [FromQuery] string? emailFuncionario,
        CancellationToken cancellationToken)
    {
        var dispositivos = await _infraestructuraService.GetDispositivosAsync(emailFuncionario, cancellationToken);
        return Ok(dispositivos);
    }

    [HttpGet("dispositivos/mios")]
    [Authorize(Roles = "Funcionario")]
    public async Task<ActionResult<List<DispositivoResponse>>> GetMisDispositivos(CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        var dispositivos = await _infraestructuraService.GetDispositivosAsync(email, cancellationToken);
        return Ok(dispositivos);
    }

    [HttpGet("dispositivos/{idDispositivo:int}")]
    [Authorize(Roles = "Admin,Funcionario")]
    public async Task<ActionResult<DispositivoResponse>> GetDispositivoById(int idDispositivo, CancellationToken cancellationToken)
    {
        var dispositivo = await _infraestructuraService.GetDispositivoByIdAsync(idDispositivo, cancellationToken);

        if (dispositivo is null)
            return this.NotFoundError("Dispositivo no encontrado.");

        if (User.IsInRole("Funcionario"))
        {
            var email = GetEmailFromToken();

            if (!string.Equals(dispositivo.EmailFuncionario, email, StringComparison.OrdinalIgnoreCase))
                return this.ForbiddenError("No puede ver un dispositivo asignado a otro funcionario.");
        }

        return Ok(dispositivo);
    }

    [HttpPost("dispositivos")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DispositivoResponse>> CrearDispositivo(
        [FromBody] CrearDispositivoRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dispositivo = await _infraestructuraService.CrearDispositivoAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetDispositivoById), new { idDispositivo = dispositivo.IdDispositivoEscaneo }, dispositivo);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
        catch (PostgresException ex)
        {
            _logger.LogWarning(ex, "Error de PostgreSQL al crear dispositivo.");
            return this.BadRequestError(ex.MessageText, "database_error");
        }
    }

    [HttpPut("dispositivos/{idDispositivo:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DispositivoResponse>> ActualizarDispositivo(
        int idDispositivo,
        [FromBody] ActualizarDispositivoRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dispositivo = await _infraestructuraService.ActualizarDispositivoAsync(idDispositivo, request, cancellationToken);
            return Ok(dispositivo);
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
            _logger.LogWarning(ex, "Error de PostgreSQL al actualizar dispositivo.");
            return this.BadRequestError(ex.MessageText, "database_error");
        }
    }

    private string? GetEmailFromToken()
    {
        return User.FindFirstValue(ClaimTypes.Email);
    }
}
