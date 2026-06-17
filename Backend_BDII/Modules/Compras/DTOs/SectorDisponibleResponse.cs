namespace Backend_BDII.Modules.Compras.DTOs;

public sealed class SectorDisponibleResponse
{
    public required string NombreSector { get; init; }
    public required int Capacidad { get; init; }
    public required int EntradasVendidas { get; init; }
    public required int EntradasDisponibles { get; init; }
    public required int CostoSector { get; init; }
    public required int CostoTotalEntrada { get; init; }
}
