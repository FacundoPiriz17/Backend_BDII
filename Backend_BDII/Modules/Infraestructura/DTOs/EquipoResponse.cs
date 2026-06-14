namespace Backend_BDII.Modules.Infraestructura.DTOs;

public sealed class EquipoResponse
{
    public required string CodigoFifa { get; init; }
    public required string Nombre { get; init; }
    public required string Grupo { get; init; }
}
