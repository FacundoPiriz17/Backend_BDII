namespace Backend_BDII.Modules.Infraestructura.DTOs;

public sealed class ActualizarEstadioRequest
{
    public required string Nombre { get; init; }
    public int? Capacidad { get; init; }
    public string? Ubicacion { get; init; }
    public string? Ciudad { get; init; }
    public required string Pais { get; init; }
}
