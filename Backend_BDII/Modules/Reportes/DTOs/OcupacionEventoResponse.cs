namespace Backend_BDII.Modules.Reportes.DTOs;

public sealed class OcupacionEventoResponse
{
    public required int IdPartido { get; init; }
    public required DateOnly Fecha { get; init; }
    public required TimeOnly Hora { get; init; }
    public required string EquipoLocal { get; init; }
    public required string EquipoVisitante { get; init; }
    public required int IdEstadio { get; init; }
    public required string Estadio { get; init; }
    public required string Ciudad { get; init; }
    public required string Pais { get; init; }
    public required int CapacidadHabilitada { get; init; }
    public required int EntradasVendidas { get; init; }
    public required double PorcentajeOcupacion { get; init; }
}
