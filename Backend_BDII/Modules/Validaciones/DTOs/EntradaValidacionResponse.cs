namespace Backend_BDII.Modules.Validaciones.DTOs;

public sealed class EntradaValidacionResponse
{
    public required int IdEntrada { get; init; }
    public required string Estado { get; init; }
    public required int CostoTotal { get; init; }
    public required int TransferenciasRestantes { get; init; }
    public required string NombreSector { get; init; }
    public required string EmailPropietarioActual { get; init; }
    public required string NombrePropietarioActual { get; init; }
    public required string PaisDocumento { get; init; }
    public required string TipoDocumento { get; init; }
    public required int NumeroDocumento { get; init; }
    public required PartidoValidacionResponse Partido { get; init; }
}
