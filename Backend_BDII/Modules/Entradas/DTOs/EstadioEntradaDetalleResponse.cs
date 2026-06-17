namespace Backend_BDII.Modules.Entradas.DTOs;

public sealed class EstadioEntradaDetalleResponse
{
    public required int IdEstadio { get; init; }
    public required string Nombre { get; init; }
    public string? Ciudad { get; init; }
    public required string Pais { get; init; }
}
