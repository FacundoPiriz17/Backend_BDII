namespace Backend_BDII.Modules.Home.DTOs;

public sealed class FuncionarioHomeResponse
{
    public required int DispositivosAsignados { get; init; }
    public required int DispositivosActivos { get; init; }
    public required int ValidacionesHoy { get; init; }
    public required int ValidacionesValidasHoy { get; init; }
    public required int ValidacionesInvalidasHoy { get; init; }
    public required List<FuncionarioHomeValidacionResponse> UltimasValidaciones { get; init; }
}
