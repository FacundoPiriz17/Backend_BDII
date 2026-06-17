namespace Backend_BDII.Modules.Infraestructura.DTOs;

public sealed class CrearSectorRequest
{
    public required string NombreSector { get; init; }
    public int? Capacidad { get; init; }
    public int? Costo { get; init; }
}
