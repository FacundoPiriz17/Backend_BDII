namespace Backend_BDII.Modules.Home.DTOs;

public sealed class GeneralHomeEntradaResponse
{
    public required int IdEntrada { get; init; }
    public required string Estado { get; init; }
    public required string NombreSector { get; init; }
    public required int IdPartido { get; init; }
    public required DateOnly FechaPartido { get; init; }
    public required TimeOnly HoraPartido { get; init; }
    public required string EquipoLocal { get; init; }
    public required string EquipoVisitante { get; init; }
    public required string Estadio { get; init; }
    public string? Ciudad { get; init; }
}
