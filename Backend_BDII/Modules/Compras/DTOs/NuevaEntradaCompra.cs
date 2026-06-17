namespace Backend_BDII.Modules.Compras.DTOs;

public sealed class NuevaEntradaCompra
{
    public required int IdPartido { get; init; }
    public required string NombreSector { get; init; }
    public required string CodigoQr { get; init; }
}
