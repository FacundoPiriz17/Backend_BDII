namespace Backend_BDII.Modules.Validaciones.DTOs;

public sealed class EstadioValidacionResponse
{
    public required int IdEstadio { get; init; }
    public required string Nombre { get; init; }
    public string? Ciudad { get; init; }
    public required string Pais { get; init; }
}
