namespace Backend_BDII.Modules.Compras.DTOs;

public sealed class CrearCompraRequest
{
    public List<CrearCompraEntradaRequest> Entradas { get; init; } = [];
}
