namespace Backend_BDII.Modules.Infraestructura.DTOs;

public sealed class ActualizarDispositivoRequest
{
    public string? Modelo { get; init; }
    public required bool Activo { get; init; }
    public required string EmailFuncionario { get; init; }
}
