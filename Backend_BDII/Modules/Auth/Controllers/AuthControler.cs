using Backend_BDII.Modules.Auth.DTOs;
using Backend_BDII.Modules.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Claims;
using Backend_BDII.Modules.Usuarios.Services;
using Backend_BDII.Modules.Usuarios.DTOs;
using Backend_BDII.Common.Responses;
namespace Backend_BDII.Modules.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUsuarioService _usuarioService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IUsuarioService usuarioService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _usuarioService = usuarioService;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _authService.RegisterAsync(request, cancellationToken);

            return Created("", new
            {
                message = "Usuario registrado correctamente."
            });
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
        catch (PostgresException ex)
        {
            _logger.LogWarning(ex, "Error de PostgreSQL al registrar usuario.");

            return this.BadRequestError(ex.MessageText, "database_error");
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LoginAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return this.UnauthorizedError(ex.Message);
        }
    }
    
    [HttpPost("refresh")]
    [Authorize]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(email))
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var response = await _authService.RefreshAsync(email, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return this.UnauthorizedError(ex.Message);
        }
    }

    [HttpPost("cambiar-contrasena")]
    [Authorize]
    public async Task<IActionResult> CambiarContrasena([FromBody] CambiarContrasenaRequest request, CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(email))
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            await _authService.CambiarContrasenaAsync(email, request.ContrasenaActual, request.ContrasenaNueva, cancellationToken);
            return Ok(new { message = "Contraseña actualizada correctamente." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return this.UnauthorizedError(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(MiPerfilResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MiPerfilResponse>> Me(CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(email))
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        var perfil = await _usuarioService.GetMiPerfilAsync(email, cancellationToken);

        if (perfil is null)
            return this.NotFoundError("Usuario no encontrado.");

        return Ok(perfil);
    }
    
}
