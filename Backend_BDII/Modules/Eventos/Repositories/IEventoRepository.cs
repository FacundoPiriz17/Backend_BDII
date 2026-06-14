using Backend_BDII.Modules.Eventos.DTOs;

namespace Backend_BDII.Modules.Eventos.Repositories;

public interface IEventoRepository
{
    Task<List<EventoResponse>> GetEventosAsync(string? pais, string? estado, CancellationToken cancellationToken = default);
    Task<EventoResponse?> GetByIdAsync(int idPartido, CancellationToken cancellationToken = default);
    Task<EventoResponse> CrearAsync(string emailAdmin, CrearEventoRequest request, CancellationToken cancellationToken = default);
    Task<EventoResponse?> ActualizarAsync(int idPartido, string emailAdmin, ActualizarEventoRequest request, CancellationToken cancellationToken = default);
    Task<EventoResponse?> CambiarEstadoAsync(int idPartido, string emailAdmin, string nuevoEstado, CancellationToken cancellationToken = default);
}
