using Backend_BDII.Modules.Eventos.DTOs;

namespace Backend_BDII.Modules.Eventos.Services;

public interface IEventoService
{
    Task<List<EventoResponse>> GetAllAsync(
        bool soloFuturos,
        string? busqueda,
        string? pais,
        string? equipo,
        string? fase,
        string? estado,
        DateOnly? desde,
        DateOnly? hasta,
        CancellationToken cancellationToken = default);
    Task<EventoResponse?> GetByIdAsync(int idPartido, CancellationToken cancellationToken = default);
    Task<EventoResponse> CrearAsync(string emailAdmin, CrearEventoRequest request, CancellationToken cancellationToken = default);
    Task<EventoResponse> ActualizarAsync(int idPartido, string emailAdmin, ActualizarEventoRequest request, CancellationToken cancellationToken = default);
    Task<EventoResponse> CambiarEstadoAsync(int idPartido, string emailAdmin, CambiarEstadoEventoRequest request, CancellationToken cancellationToken = default);
}
