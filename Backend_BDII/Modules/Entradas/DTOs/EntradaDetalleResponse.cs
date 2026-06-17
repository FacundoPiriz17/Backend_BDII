namespace Backend_BDII.Modules.Entradas.DTOs;

public sealed class EntradaDetalleResponse
{
    public required int IdEntrada { get; init; }
    public required DateTime FechaHora { get; init; }
    public required string Estado { get; init; }
    public string? CodigoQr { get; init; }
    public required int CostoTotal { get; init; }
    public required int TransferenciasRestantes { get; init; }
    public required int IdCompra { get; init; }
    public required string EstadoCompra { get; init; }
    public required string NombreSector { get; init; }
    public required string EmailPropietarioActual { get; init; }
    public required string NombrePropietarioActual { get; init; }
    public required PartidoEntradaDetalleResponse Partido { get; init; }
}
