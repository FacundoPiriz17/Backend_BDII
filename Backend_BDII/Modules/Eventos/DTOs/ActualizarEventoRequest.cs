namespace Backend_BDII.Modules.Eventos.DTOs;

public sealed class ActualizarEventoRequest
{
    public required DateOnly Fecha { get; init; }
    public required TimeOnly Hora { get; init; }
    public required int IdEstadio { get; init; }
    public required string EquipoVisitante { get; init; }
    public required string EquipoLocal { get; init; }
    public required int MarcadorLocal { get; init; }
    public required int MarcadorVisitante { get; init; }
    public required int Costo { get; init; }
    public required string Fase { get; init; }
    public required string Estado { get; init; }
    public required DateOnly FechaHabilitacion { get; init; }
    public List<string> SectoresHabilitados { get; init; } = [];
}
