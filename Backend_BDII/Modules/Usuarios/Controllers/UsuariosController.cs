using Backend_BDII.Modules.Usuarios.DTOs;
using Backend_BDII.Modules.Usuarios.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_BDII.Modules.Usuarios.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize]
public sealed class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<UsuarioResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var usuarios = await _usuarioService.GetAllAsync(cancellationToken);
        return Ok(usuarios);
    }

    [HttpGet("{email}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UsuarioResponse>> GetByEmail(string email, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioService.GetByEmailAsync(email, cancellationToken);

        if (usuario is null)
            return NotFound(new { error = "Usuario no encontrado." });

        return Ok(usuario);
    }
}