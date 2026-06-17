namespace Backend_BDII.Modules.Infraestructura.DTOs;

public sealed class CrearEstadioRequest
{
    public required string Nombre { get; init; }
    public int? Capacidad { get; init; }
    public string? Ubicacion { get; init; }
    public string? Ciudad { get; init; }
    public required string Pais { get; init; }
    public List<CrearSectorRequest> Sectores { get; init; } = [];
}
