namespace Backend_BDII.Modules.Compras.DTOs;

public sealed class CrearCompraEntradaRequest
{
    public int IdPartido { get; init; }
    public required string NombreSector { get; init; }
    public int Cantidad { get; init; } = 1;
}
