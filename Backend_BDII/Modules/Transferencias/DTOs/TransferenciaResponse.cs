namespace Backend_BDII.Modules.Transferencias.DTOs;

public sealed class TransferenciaResponse
{
    public required int IdTransferencia { get; init; }
    public required DateTime FechaHora { get; init; }
    public required string EmailOrigen { get; init; }
    public required string EmailDestino { get; init; }
    public required string Estado { get; init; }
    public required EntradaTransferenciaResponse Entrada { get; init; }
}
