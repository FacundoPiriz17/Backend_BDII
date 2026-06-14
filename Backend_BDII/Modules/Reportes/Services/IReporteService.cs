using Backend_BDII.Modules.Reportes.DTOs;

namespace Backend_BDII.Modules.Reportes.Services;

public interface IReporteService
{
    Task<List<EventoMasVendidoResponse>> GetEventosMasVendidosAsync(string? pais, int? limit, CancellationToken cancellationToken = default);
    Task<List<MayorCompradorResponse>> GetMayoresCompradoresAsync(int? limit, CancellationToken cancellationToken = default);
    Task<List<OcupacionEventoResponse>> GetOcupacionEventosAsync(string? pais, CancellationToken cancellationToken = default);
    Task<ResumenValidacionesResponse> GetResumenValidacionesAsync(CancellationToken cancellationToken = default);
}
