namespace Backend_BDII.Modules.Entradas.DTOs;

public sealed class PartidoEntradaDetalleResponse
{
    public required int IdPartido { get; init; }
    public required DateOnly Fecha { get; init; }
    public required TimeOnly Hora { get; init; }
    public required string EquipoLocal { get; init; }
    public required string EquipoVisitante { get; init; }
    public required string Fase { get; init; }
    public required string Estado { get; init; }
    public required EstadioEntradaDetalleResponse Estadio { get; init; }
}
