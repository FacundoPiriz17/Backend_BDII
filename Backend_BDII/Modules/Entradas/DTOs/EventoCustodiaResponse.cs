namespace Backend_BDII.Modules.Entradas.DTOs;

public sealed class EventoCustodiaResponse
{
    public required string Tipo { get; init; }
    public required int IdReferencia { get; init; }
    public required DateTime FechaHora { get; init; }
    public required string Estado { get; init; }
    public string? EmailOrigen { get; init; }
    public string? EmailDestino { get; init; }
    public string? Detalle { get; init; }
}
