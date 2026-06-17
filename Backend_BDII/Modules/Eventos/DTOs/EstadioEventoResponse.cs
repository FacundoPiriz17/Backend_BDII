namespace Backend_BDII.Modules.Eventos.DTOs;

public sealed class EstadioEventoResponse
{
    public required int IdEstadio { get; init; }
    public required string Nombre { get; init; }
    public int? Capacidad { get; init; }
    public string? Ubicacion { get; init; }
    public string? Ciudad { get; init; }
    public required string Pais { get; init; }
}
