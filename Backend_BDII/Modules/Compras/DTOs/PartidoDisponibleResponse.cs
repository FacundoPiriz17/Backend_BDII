namespace Backend_BDII.Modules.Compras.DTOs;

public sealed class PartidoDisponibleResponse
{
    public required int IdPartido { get; init; }
    public required DateOnly Fecha { get; init; }
    public required TimeOnly Hora { get; init; }
    public required string EquipoLocal { get; init; }
    public required string EquipoVisitante { get; init; }
    public required string Fase { get; init; }
    public required string Estado { get; init; }
    public required int CostoBase { get; init; }
    public required EstadioEntradaResponse Estadio { get; init; }
    public required List<SectorDisponibleResponse> Sectores { get; init; }
}
