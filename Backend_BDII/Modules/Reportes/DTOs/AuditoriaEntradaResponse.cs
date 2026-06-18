namespace Backend_BDII.Modules.Reportes.DTOs;

public sealed class AuditoriaEntradaResponse
{
    public required string Tipo { get; init; }          // compra | transferencia | validacion
    public required DateTime Fecha { get; init; }
    public required string Usuario { get; init; }       // actor principal del evento
    public string? Estado { get; init; }
    public int? Monto { get; init; }                    // solo compras
    public string? Detalle { get; init; }
    public required int IdReferencia { get; init; }
}
