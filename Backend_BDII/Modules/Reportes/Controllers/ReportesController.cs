using Backend_BDII.Modules.Reportes.DTOs;
using Backend_BDII.Modules.Reportes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_BDII.Modules.Reportes.Controllers;

[ApiController]
[Route("api/reportes")]
[Authorize(Roles = "Admin")]
public sealed class ReportesController : ControllerBase
{
    private readonly IReporteService _reporteService;

    public ReportesController(IReporteService reporteService)
    {
        _reporteService = reporteService;
    }

    [HttpGet("eventos-mas-vendidos")]
    public async Task<ActionResult<List<EventoMasVendidoResponse>>> GetEventosMasVendidos(
        [FromQuery] string? pais,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var reporte = await _reporteService.GetEventosMasVendidosAsync(pais, limit, cancellationToken);
        return Ok(reporte);
    }

    [HttpGet("mayores-compradores")]
    public async Task<ActionResult<List<MayorCompradorResponse>>> GetMayoresCompradores(
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var reporte = await _reporteService.GetMayoresCompradoresAsync(limit, cancellationToken);
        return Ok(reporte);
    }

    [HttpGet("ocupacion-eventos")]
    public async Task<ActionResult<List<OcupacionEventoResponse>>> GetOcupacionEventos(
        [FromQuery] string? pais,
        CancellationToken cancellationToken)
    {
        var reporte = await _reporteService.GetOcupacionEventosAsync(pais, cancellationToken);
        return Ok(reporte);
    }

    [HttpGet("validaciones/resumen")]
    public async Task<ActionResult<ResumenValidacionesResponse>> GetResumenValidaciones(CancellationToken cancellationToken)
    {
        var reporte = await _reporteService.GetResumenValidacionesAsync(cancellationToken);
        return Ok(reporte);
    }
}
