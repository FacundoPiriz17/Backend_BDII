using Backend_BDII.Modules.Reportes.DTOs;
using Backend_BDII.Modules.Reportes.Repositories;

namespace Backend_BDII.Modules.Reportes.Services;

public sealed class ReporteService : IReporteService
{
    private const int DefaultLimit = 10;
    private const int MaxLimit = 100;

    private readonly IReporteRepository _reporteRepository;

    public ReporteService(IReporteRepository reporteRepository)
    {
        _reporteRepository = reporteRepository;
    }

    public Task<List<EventoMasVendidoResponse>> GetEventosMasVendidosAsync(
        string? pais,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        return _reporteRepository.GetEventosMasVendidosAsync(pais, NormalizeLimit(limit), cancellationToken);
    }

    public Task<List<MayorCompradorResponse>> GetMayoresCompradoresAsync(
        int? limit,
        CancellationToken cancellationToken = default)
    {
        return _reporteRepository.GetMayoresCompradoresAsync(NormalizeLimit(limit), cancellationToken);
    }

    public Task<List<OcupacionEventoResponse>> GetOcupacionEventosAsync(
        string? pais,
        CancellationToken cancellationToken = default)
    {
        return _reporteRepository.GetOcupacionEventosAsync(pais, cancellationToken);
    }

    public Task<ResumenValidacionesResponse> GetResumenValidacionesAsync(CancellationToken cancellationToken = default)
    {
        return _reporteRepository.GetResumenValidacionesAsync(cancellationToken);
    }

    private static int NormalizeLimit(int? limit)
    {
        if (limit is null || limit.Value <= 0)
            return DefaultLimit;

        return Math.Min(limit.Value, MaxLimit);
    }
}
