namespace Backend_BDII.Modules.Entradas.DTOs;

public sealed class CustodiaEntradaResponse
{
    public required int IdEntrada { get; init; }
    public required string EmailPropietarioActual { get; init; }
    public required int TransferenciasRestantes { get; init; }
    public required List<EventoCustodiaResponse> Eventos { get; init; }
}
