namespace Backend_BDII.Modules.Infraestructura.DTOs;

public sealed class CrearDispositivoRequest
{
    public string? Modelo { get; init; }
    public required string EmailFuncionario { get; init; }
    public bool Activo { get; init; } = true;
}
