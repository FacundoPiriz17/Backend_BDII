namespace Backend_BDII.Modules.Validaciones.DTOs;

public sealed class VerificacionManualResponse
{
    public required bool DocumentoCoincide { get; init; }
    public required EntradaValidacionResponse Entrada { get; init; }
}
