using Backend_BDII.Modules.Eventos.DTOs;
using Backend_BDII.Modules.Eventos.Repositories;

namespace Backend_BDII.Modules.Eventos.Services;

public sealed class EventoService : IEventoService
{
    private static readonly HashSet<string> SectoresValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "A",
        "B",
        "C",
        "D"
    };

    private static readonly HashSet<string> FasesValidas = new(StringComparer.Ordinal)
    {
        "Fase de grupos",
        "Dieciseisavos de final",
        "Octavos de final",
        "Cuartos de final",
        "Semifinal",
        "Final"
    };

    private static readonly HashSet<string> EstadosValidos = new(StringComparer.Ordinal)
    {
        "no empezado",
        "empezado",
        "terminado"
    };

    private readonly IEventoRepository _eventoRepository;

    public EventoService(IEventoRepository eventoRepository)
    {
        _eventoRepository = eventoRepository;
    }

    public Task<List<EventoResponse>> GetEventosAsync(string? pais, string? estado, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(estado))
            ValidarEstado(estado);

        return _eventoRepository.GetEventosAsync(pais, estado, cancellationToken);
    }

    public Task<EventoResponse?> GetByIdAsync(int idPartido, CancellationToken cancellationToken = default)
    {
        return _eventoRepository.GetByIdAsync(idPartido, cancellationToken);
    }

    public Task<EventoResponse> CrearAsync(
        string emailAdmin,
        CrearEventoRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidarEquipos(request.EquipoLocal, request.EquipoVisitante);
        ValidarFase(request.Fase);
        ValidarSectores(request.SectoresHabilitados);

        return _eventoRepository.CrearAsync(NormalizeEmail(emailAdmin), request, cancellationToken);
    }

    public async Task<EventoResponse> ActualizarAsync(
        int idPartido,
        string emailAdmin,
        ActualizarEventoRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidarEquipos(request.EquipoLocal, request.EquipoVisitante);
        ValidarFase(request.Fase);
        ValidarEstado(request.Estado);
        ValidarSectores(request.SectoresHabilitados);

        return await _eventoRepository.ActualizarAsync(idPartido, NormalizeEmail(emailAdmin), request, cancellationToken)
               ?? throw new KeyNotFoundException("Evento no encontrado.");
    }

    public async Task<EventoResponse> CambiarEstadoAsync(
        int idPartido,
        string emailAdmin,
        string nuevoEstado,
        CancellationToken cancellationToken = default)
    {
        ValidarEstado(nuevoEstado);

        return await _eventoRepository.CambiarEstadoAsync(idPartido, NormalizeEmail(emailAdmin), nuevoEstado, cancellationToken)
               ?? throw new KeyNotFoundException("Evento no encontrado.");
    }

    private static void ValidarEquipos(string equipoLocal, string equipoVisitante)
    {
        if (string.Equals(equipoLocal.Trim(), equipoVisitante.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El equipo local y el visitante no pueden ser el mismo.");
    }

    private static void ValidarFase(string fase)
    {
        if (!FasesValidas.Contains(fase.Trim()))
            throw new InvalidOperationException(
                "La fase debe ser una de: Fase de grupos, Dieciseisavos de final, Octavos de final, Cuartos de final, Semifinal, Final.");
    }

    private static void ValidarEstado(string estado)
    {
        if (!EstadosValidos.Contains(estado.Trim()))
            throw new InvalidOperationException("El estado debe ser: no empezado, empezado o terminado.");
    }

    private static void ValidarSectores(IReadOnlyCollection<string> sectores)
    {
        if (sectores.Count == 0)
            throw new InvalidOperationException("Debe habilitar al menos un sector.");

        foreach (var sector in sectores)
        {
            if (!SectoresValidos.Contains(sector.Trim()))
                throw new InvalidOperationException("Los sectores habilitados deben ser A, B, C o D.");
        }
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
