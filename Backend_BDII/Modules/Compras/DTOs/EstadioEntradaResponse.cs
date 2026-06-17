namespace Backend_BDII.Modules.Compras.DTOs;

public sealed class EstadioEntradaResponse
{
    public required int IdEstadio { get; init; }
    public required string Nombre { get; init; }
    public string? Ciudad { get; init; }
    public required string Pais { get; init; }
}
