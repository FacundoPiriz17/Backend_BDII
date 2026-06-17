using System.Security.Claims;
using Backend_BDII.Common.Responses;
using Backend_BDII.Modules.Home.DTOs;
using Backend_BDII.Modules.Home.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_BDII.Modules.Home.Controllers;

[ApiController]
[Route("api/home")]
[Authorize]
public sealed class HomeController : ControllerBase
{
    private readonly IHomeService _homeService;

    public HomeController(IHomeService homeService)
    {
        _homeService = homeService;
    }

    [HttpGet("general")]
    [Authorize(Roles = "General")]
    public async Task<ActionResult<GeneralHomeResponse>> General(CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        return Ok(await _homeService.GetGeneralAsync(email, cancellationToken));
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AdminHomeResponse>> Admin(CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        return Ok(await _homeService.GetAdminAsync(email, cancellationToken));
    }

    [HttpGet("funcionario")]
    [Authorize(Roles = "Funcionario")]
    public async Task<ActionResult<FuncionarioHomeResponse>> Funcionario(CancellationToken cancellationToken)
    {
        var email = GetEmailFromToken();

        if (email is null)
            return this.UnauthorizedError("No se pudo obtener el email del token.");

        return Ok(await _homeService.GetFuncionarioAsync(email, cancellationToken));
    }

    private string? GetEmailFromToken()
    {
        return User.FindFirstValue(ClaimTypes.Email);
    }
}
