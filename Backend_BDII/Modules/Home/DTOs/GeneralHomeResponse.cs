namespace Backend_BDII.Modules.Home.DTOs;

public sealed class GeneralHomeResponse
{
    public required int ComprasPagas { get; init; }
    public required int EntradasActivas { get; init; }
    public required int TransferenciasPendientesRecibidas { get; init; }
    public required int TransferenciasPendientesEnviadas { get; init; }
    public required List<GeneralHomeEntradaResponse> ProximasEntradas { get; init; }
    public required List<GeneralHomeTransferenciaResponse> TransferenciasPendientes { get; init; }
}
