using Backend_BDII.Modules.Reportes.DTOs;

namespace Backend_BDII.Modules.Reportes.Repositories;

public interface IReporteRepository
{
    Task<List<EventoMasVendidoResponse>> GetEventosMasVendidosAsync(int limit, CancellationToken cancellationToken = default);
    Task<List<MayorCompradorResponse>> GetMayoresCompradoresAsync(int limit, CancellationToken cancellationToken = default);
    Task<List<OcupacionEventoResponse>> GetOcupacionEventosAsync(int limit, CancellationToken cancellationToken = default);
    Task<ResumenValidacionesResponse> GetResumenValidacionesAsync(CancellationToken cancellationToken = default);
    Task<List<AuditoriaEntradaResponse>> GetAuditoriaAsync(string? tipo, int limit, CancellationToken cancellationToken = default);
}
