namespace Backend_BDII.Modules.Compras.DTOs;

public sealed class CompraResponse
{
    public required int IdCompra { get; init; }
    public required DateTime FechaHora { get; init; }
    public required int MontoTotal { get; init; }
    public required double PorcentajeComision { get; init; }
    public required string EmailUsuario { get; init; }
    public required string Estado { get; init; }
    public required List<EntradaResponse> Entradas { get; init; }
}
