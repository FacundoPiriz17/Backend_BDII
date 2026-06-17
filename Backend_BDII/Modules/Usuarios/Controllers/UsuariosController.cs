using System.Security.Claims;
using Backend_BDII.Common.Responses;
using Backend_BDII.Modules.Usuarios.DTOs;
using Backend_BDII.Modules.Usuarios.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Backend_BDII.Modules.Usuarios.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize]
public sealed class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;
    private readonly ILogger<UsuariosController> _logger;

    public UsuariosController(IUsuarioService usuarioService, ILogger<UsuariosController> logger)
    {
        _usuarioService = usuarioService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<UsuarioResponse>>> GetAll(
        [FromQuery] string? rol,
        [FromQuery] bool? habilitado,
        [FromQuery] string? busqueda,
        CancellationToken cancellationToken)
    {
        try
        {
            var usuarios = await _usuarioService.GetAllAsync(rol, habilitado, busqueda, cancellationToken);
            return Ok(usuarios);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
    }

    /// <summary>
    /// Permite a cualquier usuario autenticado (General, Funcionario o Admin)
    /// editar sus propios datos de contacto. No expone documento ni habilitacion.
    /// </summary>
    [HttpPut("me")]
    public async Task<ActionResult<MiPerfilResponse>> ActualizarMiPerfil(
        [FromBody] ActualizarMiPerfilRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var perfil = await _usuarioService.ActualizarMiPerfilAsync(email, request, cancellationToken);
            return Ok(perfil);
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
            _logger.LogWarning(ex, "Error de PostgreSQL al actualizar el perfil propio.");
            return this.BadRequestError(ex.MessageText, "database_error");
        }
    }

    [HttpGet("{email}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UsuarioResponse>> GetByEmail(string email, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioService.GetByEmailAsync(email, cancellationToken);

        if (usuario is null)
            return this.NotFoundError("Usuario no encontrado.");

        return Ok(usuario);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UsuarioResponse>> Crear(
        [FromBody] CrearUsuarioAdminRequest request,
        CancellationToken cancellationToken)
    {
        var emailAdmin = GetEmailFromToken();

        if (emailAdmin is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var usuario = await _usuarioService.CrearAsync(emailAdmin, request, cancellationToken);
            return CreatedAtAction(nameof(GetByEmail), new { email = usuario.Email }, usuario);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
        catch (PostgresException ex)
        {
            _logger.LogWarning(ex, "Error de PostgreSQL al crear usuario desde administracion.");
            return this.BadRequestError(ex.MessageText, "database_error");
        }
    }

    [HttpPut("{email}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UsuarioResponse>> Actualizar(
        string email,
        [FromBody] ActualizarUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var emailAdmin = GetEmailFromToken();

        if (emailAdmin is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var usuario = await _usuarioService.ActualizarAsync(emailAdmin, email, request, cancellationToken);
            return Ok(usuario);
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
            _logger.LogWarning(ex, "Error de PostgreSQL al actualizar usuario.");
            return this.BadRequestError(ex.MessageText, "database_error");
        }
    }

    [HttpPatch("{email}/habilitacion")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UsuarioResponse>> ActualizarHabilitacion(
        string email,
        [FromBody] ActualizarHabilitacionUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var emailAdmin = GetEmailFromToken();

        if (emailAdmin is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var usuario = await _usuarioService.ActualizarHabilitacionAsync(emailAdmin, email, request.Habilitado, cancellationToken);
            return Ok(usuario);
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFoundError(ex.Message);
        }
    }

    [HttpPut("{email}/roles")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UsuarioResponse>> ActualizarRoles(
        string email,
        [FromBody] ActualizarRolesUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var emailAdmin = GetEmailFromToken();

        if (emailAdmin is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        try
        {
            var usuario = await _usuarioService.ActualizarRolesAsync(emailAdmin, email, request, cancellationToken);
            return Ok(usuario);
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
            _logger.LogWarning(ex, "Error de PostgreSQL al actualizar roles de usuario.");
            return this.BadRequestError(ex.MessageText, "database_error");
        }
    }

    private string? GetEmailFromToken()
    {
        return User.FindFirstValue(ClaimTypes.Email);
    }
}
