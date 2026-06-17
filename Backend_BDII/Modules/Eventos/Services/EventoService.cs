using Backend_BDII.Common.Auditing;
using Backend_BDII.Modules.Eventos.DTOs;
using Backend_BDII.Modules.Eventos.Repositories;

namespace Backend_BDII.Modules.Eventos.Services;

public sealed class EventoService : IEventoService
{
    private static readonly HashSet<string> FasesValidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "Fase de grupos",
        "Dieciseisavos de final",
        "Octavos de final",
        "Cuartos de final",
        "Semifinal",
        "Final"
    };

    private static readonly HashSet<string> EstadosValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "terminado",
        "empezado",
        "no empezado"
    };

    private static readonly HashSet<string> SectoresValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "A",
        "B",
        "C",
        "D"
    };

    private readonly IEventoRepository _eventoRepository;
    private readonly IAuditService _auditService;

    public EventoService(IEventoRepository eventoRepository, IAuditService auditService)
    {
        _eventoRepository = eventoRepository;
        _auditService = auditService;
    }

    public Task<List<EventoResponse>> GetAllAsync(
        bool soloFuturos,
        string? busqueda,
        string? pais,
        string? equipo,
        string? fase,
        string? estado,
        DateOnly? desde,
        DateOnly? hasta,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(fase) && !FasesValidas.Contains(fase.Trim()))
            throw new InvalidOperationException("La fase del partido no es valida.");

        if (!string.IsNullOrWhiteSpace(estado) && !EstadosValidos.Contains(estado.Trim()))
            throw new InvalidOperationException("El estado del partido no es valido.");

        if (desde.HasValue && hasta.HasValue && desde > hasta)
            throw new InvalidOperationException("La fecha desde no puede ser posterior a la fecha hasta.");

        return _eventoRepository.GetAllAsync(soloFuturos, busqueda, pais, equipo, fase, estado, desde, hasta, cancellationToken);
    }

    public Task<EventoResponse?> GetByIdAsync(int idPartido, CancellationToken cancellationToken = default)
    {
        return _eventoRepository.GetByIdAsync(idPartido, cancellationToken);
    }

    public async Task<EventoResponse> CrearAsync(
        string emailAdmin,
        CrearEventoRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(emailAdmin);
        ValidarDatosEvento(request.EquipoLocal, request.EquipoVisitante, request.Costo, request.Fase, request.SectoresHabilitados);
        var evento = await _eventoRepository.CrearAsync(email, request, cancellationToken);

        _auditService.Record("evento.crear", email, new
        {
            evento.IdPartido,
            evento.EquipoLocal,
            evento.EquipoVisitante
        });

        return evento;
    }

    public async Task<EventoResponse> ActualizarAsync(
        int idPartido,
        string emailAdmin,
        ActualizarEventoRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidarDatosEvento(request.EquipoLocal, request.EquipoVisitante, request.Costo, request.Fase, request.SectoresHabilitados);

        if (!EstadosValidos.Contains(request.Estado))
            throw new InvalidOperationException("El estado del partido no es valido.");

        if (request.MarcadorLocal < 0 || request.MarcadorVisitante < 0)
            throw new InvalidOperationException("Los marcadores no pueden ser negativos.");

        var email = NormalizeEmail(emailAdmin);
        var evento = await _eventoRepository.ActualizarAsync(
                   idPartido,
                   email,
                   request,
                   cancellationToken)
               ?? throw new KeyNotFoundException("Evento no encontrado.");

        _auditService.Record("evento.actualizar", email, new { evento.IdPartido });

        return evento;
    }

    public async Task<EventoResponse> CambiarEstadoAsync(
        int idPartido,
        string emailAdmin,
        CambiarEstadoEventoRequest request,
        CancellationToken cancellationToken = default)
    {
        var estado = request.Estado.Trim().ToLowerInvariant();

        if (!EstadosValidos.Contains(estado))
            throw new InvalidOperationException("El estado debe ser terminado, empezado o no empezado.");

        var email = NormalizeEmail(emailAdmin);
        var evento = await _eventoRepository.CambiarEstadoAsync(
                   idPartido,
                   email,
                   estado,
                   cancellationToken)
               ?? throw new KeyNotFoundException("Evento no encontrado.");

        _auditService.Record("evento.estado", email, new
        {
            evento.IdPartido,
            evento.Estado
        });

        return evento;
    }

    private static void ValidarDatosEvento(
        string equipoLocal,
        string equipoVisitante,
        int costo,
        string fase,
        List<string>? sectores)
    {
        if (string.IsNullOrWhiteSpace(equipoLocal) || string.IsNullOrWhiteSpace(equipoVisitante))
            throw new InvalidOperationException("Los equipos local y visitante son obligatorios.");

        if (equipoLocal.Trim().Equals(equipoVisitante.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El equipo local y visitante deben ser distintos.");

        if (costo < 0)
            throw new InvalidOperationException("El costo base no puede ser negativo.");

        if (!FasesValidas.Contains(fase))
            throw new InvalidOperationException("La fase del partido no es valida.");

        if (sectores is null || sectores.Count == 0)
            throw new InvalidOperationException("Debe habilitar al menos un sector para el evento.");

        if (sectores.Any(s => string.IsNullOrWhiteSpace(s) || !SectoresValidos.Contains(s.Trim())))
            throw new InvalidOperationException("Los sectores habilitados deben ser A, B, C o D.");
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
