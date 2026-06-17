namespace Backend_BDII.Modules.Home.DTOs;

public sealed class AdminHomeEventoResponse
{
    public required int IdPartido { get; init; }
    public required DateOnly Fecha { get; init; }
    public required TimeOnly Hora { get; init; }
    public required string EquipoLocal { get; init; }
    public required string EquipoVisitante { get; init; }
    public required string Estadio { get; init; }
    public required int EntradasVendidas { get; init; }
}
