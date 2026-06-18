namespace Backend_BDII.Modules.Auth.DTOs;

public sealed class CambiarContrasenaRequest
{
    public required string ContrasenaActual { get; init; }
    public required string ContrasenaNueva { get; init; }
}
