namespace Backend_BDII.Modules.Reportes.DTOs;

public sealed class ResumenValidacionesResponse
{
    public required int TotalValidaciones { get; init; }
    public required int ValidacionesValidas { get; init; }
    public required int ValidacionesInvalidas { get; init; }
    public required int EntradasConsumidas { get; init; }
}
