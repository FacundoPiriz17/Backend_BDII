using Backend_BDII.Modules.Eventos.DTOs;

namespace Backend_BDII.Modules.Eventos.Repositories;

/// <summary>Contexto para validar la creación/edición de un evento.</summary>
public sealed record EventoCreacionContexto(
    string? PaisAdmin,
    string? PaisEstadio,
    string? GrupoLocal,
    string? GrupoVisitante);

public interface IEventoRepository
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

    /// <summary>País del admin, país del estadio y grupo de cada equipo, para
    /// validar jurisdicción y coincidencia de grupo al crear/editar eventos.</summary>
    Task<EventoCreacionContexto> GetContextoEventoAsync(
        string emailAdmin, int idEstadio, string equipoLocal, string equipoVisitante,
        CancellationToken cancellationToken = default);
    Task<EventoResponse> CrearAsync(string emailAdmin, CrearEventoRequest request, CancellationToken cancellationToken = default);
    Task<EventoResponse?> ActualizarAsync(int idPartido, string emailAdmin, ActualizarEventoRequest request, CancellationToken cancellationToken = default);
    Task<EventoResponse?> CambiarEstadoAsync(int idPartido, string emailAdmin, string estado, CancellationToken cancellationToken = default);
}
