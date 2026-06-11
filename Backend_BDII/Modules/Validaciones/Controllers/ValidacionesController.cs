using System.Security.Claims;
using Backend_BDII.Common.Responses;
using Backend_BDII.Modules.Validaciones.DTOs;
using Backend_BDII.Modules.Validaciones.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Backend_BDII.Modules.Validaciones.Controllers;

[ApiController]
[Route("api/validaciones")]
[Authorize(Roles = "Admin,Funcionario")]
public sealed class ValidacionesController : ControllerBase
{
    private readonly IValidacionService _validacionService;
    private readonly ILogger<ValidacionesController> _logger;

    public ValidacionesController(
        IValidacionService validacionService,
        ILogger<ValidacionesController> logger)
    {
        _validacionService = validacionService;
        _logger = logger;
    }

    [HttpPost("escanear")]
    [Authorize(Roles = "Funcionario")]
    public async Task<ActionResult<ValidacionResponse>> EscanearQr(
        [FromBody] EscanearQrRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var validacion = await _validacionService.EscanearQrAsync(email, request, cancellationToken);
            return Ok(validacion);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            return this.BadRequestError("La entrada ya tiene una validacion valida registrada.");
        }
        catch (PostgresException ex)
        {
            _logger.LogWarning(ex, "Error de PostgreSQL al escanear QR.");
            return this.BadRequestError(ex.MessageText, "database_error");
        }
    }

    [HttpPost("invalidar")]
    [Authorize(Roles = "Funcionario")]
    public async Task<ActionResult<ValidacionResponse>> RegistrarInvalidacion(
        [FromBody] RegistrarInvalidacionRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var validacion = await _validacionService.RegistrarInvalidacionAsync(email, request, cancellationToken);
            return Ok(validacion);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
        catch (PostgresException ex)
        {
            _logger.LogWarning(ex, "Error de PostgreSQL al registrar invalidacion.");
            return this.BadRequestError(ex.MessageText, "database_error");
        }
    }

    [HttpPost("manual/verificar")]
    [Authorize(Roles = "Funcionario")]
    public async Task<ActionResult<VerificacionManualResponse>> VerificarEntradaManual(
        [FromBody] VerificarEntradaManualRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var verificacion = await _validacionService.VerificarEntradaManualAsync(request, cancellationToken);
            return Ok(verificacion);
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFoundError(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<ValidacionResponse>>> GetHistorial(
        [FromQuery] int? idPartido,
        [FromQuery] int? idEntrada,
        [FromQuery] string? estado,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        string? emailFuncionario = null;

        if (User.IsInRole("Funcionario"))
        {
            emailFuncionario = GetEmailFromToken();

            if (emailFuncionario is null)
                return this.UnauthorizedError("No se pudo obtener el email del token.");
        }

        try
        {
            var validaciones = await _validacionService.GetHistorialAsync(
                emailFuncionario,
                idPartido,
                idEntrada,
                estado,
                limit,
                cancellationToken);

            return Ok(validaciones);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
    }

    [HttpGet("{idValidacion:int}")]
    public async Task<ActionResult<ValidacionResponse>> GetById(int idValidacion, CancellationToken cancellationToken)
    {
        var validacion = await _validacionService.GetByIdAsync(idValidacion, cancellationToken);

        if (validacion is null)
            return this.NotFoundError("Validacion no encontrada.");

        if (User.IsInRole("Funcionario"))
        {
            var email = GetEmailFromToken();

            if (!string.Equals(validacion.Funcionario.Email, email, StringComparison.OrdinalIgnoreCase))
                return this.ForbiddenError("No puede ver validaciones de otro funcionario.");
        }

        return Ok(validacion);
    }

    private string? GetEmailFromToken()
    {
        return User.FindFirstValue(ClaimTypes.Email);
    }
}
