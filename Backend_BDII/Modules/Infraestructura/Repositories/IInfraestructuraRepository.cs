using Backend_BDII.Modules.Infraestructura.DTOs;

namespace Backend_BDII.Modules.Infraestructura.Repositories;

public interface IInfraestructuraRepository
{
    Task<List<EstadioResponse>> GetEstadiosAsync(string? pais, CancellationToken cancellationToken = default);
    Task<EstadioResponse?> GetEstadioByIdAsync(int idEstadio, CancellationToken cancellationToken = default);
    Task<EstadioResponse> CrearEstadioAsync(string emailAdmin, CrearEstadioRequest request, CancellationToken cancellationToken = default);
    Task<EstadioResponse?> ActualizarEstadioAsync(int idEstadio, string emailAdmin, ActualizarEstadioRequest request, CancellationToken cancellationToken = default);
    Task<SectorInfraestructuraResponse?> ActualizarSectorAsync(int idEstadio, string nombreSector, string emailAdmin, ActualizarSectorRequest request, CancellationToken cancellationToken = default);
    Task<List<EquipoResponse>> GetEquiposAsync(CancellationToken cancellationToken = default);
    Task<List<DispositivoResponse>> GetDispositivosAsync(string? emailFuncionario, CancellationToken cancellationToken = default);
    Task<DispositivoResponse?> GetDispositivoByIdAsync(int idDispositivo, CancellationToken cancellationToken = default);
    Task<DispositivoResponse> CrearDispositivoAsync(CrearDispositivoRequest request, CancellationToken cancellationToken = default);
    Task<DispositivoResponse?> ActualizarDispositivoAsync(int idDispositivo, ActualizarDispositivoRequest request, CancellationToken cancellationToken = default);
}
