namespace Backend_BDII.Modules.Validaciones.DTOs;

public sealed class VerificarEntradaManualRequest
{
    public required int IdEntrada { get; init; }
    public required int NumeroDocumento { get; init; }
}
