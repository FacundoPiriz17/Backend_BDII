using Backend_BDII.Modules.Reportes.DTOs;
using Backend_BDII.Modules.Reportes.Repositories;

namespace Backend_BDII.Modules.Reportes.Services;

public sealed class ReporteService : IReporteService
{
    private readonly IReporteRepository _reporteRepository;

    public ReporteService(IReporteRepository reporteRepository)
    {
        _reporteRepository = reporteRepository;
    }

    public Task<List<EventoMasVendidoResponse>> GetEventosMasVendidosAsync(int limit, CancellationToken cancellationToken = default)
    {
        return _reporteRepository.GetEventosMasVendidosAsync(NormalizeLimit(limit), cancellationToken);
    }

    public Task<List<MayorCompradorResponse>> GetMayoresCompradoresAsync(int limit, CancellationToken cancellationToken = default)
    {
        return _reporteRepository.GetMayoresCompradoresAsync(NormalizeLimit(limit), cancellationToken);
    }

    public Task<List<OcupacionEventoResponse>> GetOcupacionEventosAsync(int limit, CancellationToken cancellationToken = default)
    {
        return _reporteRepository.GetOcupacionEventosAsync(NormalizeLimit(limit), cancellationToken);
    }

    public Task<ResumenValidacionesResponse> GetResumenValidacionesAsync(CancellationToken cancellationToken = default)
    {
        return _reporteRepository.GetResumenValidacionesAsync(cancellationToken);
    }

    private static int NormalizeLimit(int limit)
    {
        return Math.Clamp(limit, 1, 100);
    }
}
