namespace Backend_BDII.Modules.Eventos.DTOs;

public sealed class EventoResponse
{
    public required int IdPartido { get; init; }
    public required DateOnly Fecha { get; init; }
    public required TimeOnly Hora { get; init; }
    public required string EquipoLocal { get; init; }
    public required string EquipoVisitante { get; init; }
    public required int MarcadorLocal { get; init; }
    public required int MarcadorVisitante { get; init; }
    public required int CostoBase { get; init; }
    public required string Fase { get; init; }
    public required string Estado { get; init; }
    public required string EmailAdmin { get; init; }
    public required DateOnly FechaHabilitacion { get; init; }
    public required EstadioEventoResponse Estadio { get; init; }
    public required List<SectorEventoResponse> Sectores { get; init; }
}
