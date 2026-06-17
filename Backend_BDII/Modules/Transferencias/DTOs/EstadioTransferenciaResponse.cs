namespace Backend_BDII.Modules.Transferencias.DTOs;

public sealed class EstadioTransferenciaResponse
{
    public required int IdEstadio { get; init; }
    public required string Nombre { get; init; }
    public string? Ciudad { get; init; }
    public required string Pais { get; init; }
}
