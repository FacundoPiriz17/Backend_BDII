namespace Backend_BDII.Modules.Home.DTOs;

public sealed class GeneralHomeTransferenciaResponse
{
    public required int IdTransferencia { get; init; }
    public required int IdEntrada { get; init; }
    public required string EmailOrigen { get; init; }
    public required string EmailDestino { get; init; }
    public required DateTime FechaHora { get; init; }
}
