namespace Backend_BDII.Modules.Validaciones.DTOs;

public sealed class ValidacionResponse
{
    public required int IdValidacion { get; init; }
    public required int IdEntrada { get; init; }
    public required int IdDispositivo { get; init; }
    public required string Estado { get; init; }
    public required string CodigoEscaneado { get; init; }
    public required DateTime FechaHora { get; init; }
    public required FuncionarioValidacionResponse Funcionario { get; init; }
    public required EntradaValidacionResponse Entrada { get; init; }
}
