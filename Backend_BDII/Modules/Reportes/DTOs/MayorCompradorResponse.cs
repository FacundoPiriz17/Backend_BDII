namespace Backend_BDII.Modules.Reportes.DTOs;

public sealed class MayorCompradorResponse
{
    public required string EmailUsuario { get; init; }
    public required string NombreUsuario { get; init; }
    public required int ComprasPagas { get; init; }
    public required int EntradasCompradas { get; init; }
    public required int MontoTotalPagado { get; init; }
}
