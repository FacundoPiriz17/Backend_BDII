namespace Backend_BDII.Modules.Eventos.DTOs;

public sealed class SectorEventoResponse
{
    public required string NombreSector { get; init; }
    public required int Capacidad { get; init; }
    public required int CostoSector { get; init; }
    public required int CostoTotalEntrada { get; init; }
    public required int EntradasVendidas { get; init; }
    public required int EntradasDisponibles { get; init; }
}
