namespace Backend_BDII.Modules.Home.DTOs;

public sealed class FuncionarioHomeValidacionResponse
{
    public required int IdValidacion { get; init; }
    public required int IdEntrada { get; init; }
    public required int IdDispositivo { get; init; }
    public required string Estado { get; init; }
    public required DateTime FechaHora { get; init; }
    public required string EquipoLocal { get; init; }
    public required string EquipoVisitante { get; init; }
}
