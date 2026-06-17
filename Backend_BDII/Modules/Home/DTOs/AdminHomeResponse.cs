namespace Backend_BDII.Modules.Home.DTOs;

public sealed class AdminHomeResponse
{
    public required int EventosTotales { get; init; }
    public required int EventosFuturos { get; init; }
    public required int EntradasVendidas { get; init; }
    public required int MontoVendido { get; init; }
    public required int ValidacionesHoy { get; init; }
    public required int ValidacionesInvalidasHoy { get; init; }
    public required List<AdminHomeEventoResponse> ProximosEventos { get; init; }
}
