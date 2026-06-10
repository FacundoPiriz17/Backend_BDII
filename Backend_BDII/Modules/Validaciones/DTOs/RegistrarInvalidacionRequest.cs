namespace Backend_BDII.Modules.Validaciones.DTOs;

public sealed class RegistrarInvalidacionRequest
{
    public required int IdDispositivo { get; init; }
    public required int IdEntrada { get; init; }
    public required string CodigoEscaneado { get; init; }
}
