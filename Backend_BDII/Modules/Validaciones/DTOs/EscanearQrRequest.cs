namespace Backend_BDII.Modules.Validaciones.DTOs;

public sealed class EscanearQrRequest
{
    public required int IdDispositivo { get; init; }
    public required string CodigoEscaneado { get; init; }
}
