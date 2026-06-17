namespace Backend_BDII.Modules.Transferencias.DTOs;

public sealed class EntradaTransferenciaResponse
{
    public required int IdEntrada { get; init; }
    public required string Estado { get; init; }
    public required int CostoTotal { get; init; }
    public required int TransferenciasRestantes { get; init; }
    public required string NombreSector { get; init; }
    public required string EmailPropietarioActual { get; init; }
    public required PartidoTransferenciaResponse Partido { get; init; }
}
