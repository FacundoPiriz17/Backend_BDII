namespace Backend_BDII.Modules.Infraestructura.DTOs;

public sealed class DispositivoResponse
{
    public required int IdDispositivoEscaneo { get; init; }
    public string? Modelo { get; init; }
    public required bool Activo { get; init; }
    public required string EmailFuncionario { get; init; }
    public required int NumeroLegajo { get; init; }
    public required string NombreFuncionario { get; init; }
}
