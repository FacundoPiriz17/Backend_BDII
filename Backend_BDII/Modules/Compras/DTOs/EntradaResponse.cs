namespace Backend_BDII.Modules.Compras.DTOs;

public sealed class EntradaResponse
{
    public required int IdEntrada { get; init; }
    public required DateTime FechaHora { get; init; }
    public required string Estado { get; init; }
    public string? CodigoQr { get; init; }
    public required int CostoTotal { get; init; }
    public required int TransferenciasRestantes { get; init; }
    public required int IdCompra { get; init; }
    public required string NombreSector { get; init; }
    public required string EmailPropietarioActual { get; init; }
    public required PartidoEntradaResponse Partido { get; init; }
}
