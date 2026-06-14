namespace Backend_BDII.Modules.Eventos.DTOs;

public sealed class CrearEventoRequest
{
    public required DateOnly Fecha { get; init; }
    public required TimeOnly Hora { get; init; }
    public required int IdEstadio { get; init; }
    public required string EquipoVisitante { get; init; }
    public required string EquipoLocal { get; init; }
    public int Costo { get; init; }
    public required string Fase { get; init; }
    public DateOnly? FechaHabilitacion { get; init; }
    public List<string> SectoresHabilitados { get; init; } = [];
}
