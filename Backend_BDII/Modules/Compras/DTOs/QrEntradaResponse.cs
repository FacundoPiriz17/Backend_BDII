namespace Backend_BDII.Modules.Compras.DTOs;

public sealed class QrEntradaResponse
{
    public required int IdEntrada { get; init; }
    public required string CodigoQr { get; init; }
    public required string QrPngBase64 { get; init; }
    public required DateTime FechaHoraGeneracion { get; init; }
}
