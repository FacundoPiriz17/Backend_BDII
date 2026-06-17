namespace Backend_BDII.Modules.Validaciones.DTOs;

public sealed class FuncionarioValidacionResponse
{
    public required string Email { get; init; }
    public required string Nombre { get; init; }
    public required int NumeroLegajo { get; init; }
}
