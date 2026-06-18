using Backend_BDII.Common.Responses;
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
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var reporte = await _reporteService.GetEventosMasVendidosAsync(limit, cancellationToken);
        return Ok(reporte);
    }

    [HttpGet("mayores-compradores")]
    public async Task<ActionResult<List<MayorCompradorResponse>>> GetMayoresCompradores(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var reporte = await _reporteService.GetMayoresCompradoresAsync(limit, cancellationToken);
        return Ok(reporte);
    }

    [HttpGet("ocupacion-eventos")]
    public async Task<ActionResult<List<OcupacionEventoResponse>>> GetOcupacionEventos(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var reporte = await _reporteService.GetOcupacionEventosAsync(limit, cancellationToken);
        return Ok(reporte);
    }

    [HttpGet("validaciones/resumen")]
    public async Task<ActionResult<ResumenValidacionesResponse>> GetResumenValidaciones(CancellationToken cancellationToken)
    {
        var reporte = await _reporteService.GetResumenValidacionesAsync(cancellationToken);
        return Ok(reporte);
    }
    
    [HttpGet("auditoria")]
    public async Task<ActionResult<List<AuditoriaEntradaResponse>>> GetAuditoria(
        [FromQuery] string? tipo,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var reporte = await _reporteService.GetAuditoriaAsync(tipo, limit, cancellationToken);
            return Ok(reporte);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestError(ex.Message);
        }
    }
}
