namespace Backend_BDII.Modules.Compras.DTOs;

public sealed class CompraResponse
{
    public required int IdCompra { get; init; }
    public required DateTime FechaHora { get; init; }
    public required int MontoTotal { get; init; }
    public required double PorcentajeComision { get; init; }
    public required string EmailUsuario { get; init; }
    public required string Estado { get; init; }
    public required List<EntradaResponse> Entradas { get; init; }
}

public sealed class EntradaResponse
{
    public required int IdEntrada { get; init; }
    public required DateTime FechaHora { get; init; }
    public required string Estado { get; init; }
    public string? CodigoQr { get; init; }
    public required int CostoTotal { get; init; }
    public required int TransferenciasRestantes { get; init; }
    public required int IdCompra { get; init; }
    public required string NombreSector { get; init; }
    public required string EmailPropietarioActual { get; init; }
    public required PartidoEntradaResponse Partido { get; init; }
}

public sealed class PartidoEntradaResponse
{
    public required int IdPartido { get; init; }
    public required DateOnly Fecha { get; init; }
    public required TimeOnly Hora { get; init; }
    public required string EquipoLocal { get; init; }
    public required string EquipoVisitante { get; init; }
    public required string Fase { get; init; }
    public required string Estado { get; init; }
    public required EstadioEntradaResponse Estadio { get; init; }
}

public sealed class EstadioEntradaResponse
{
    public required int IdEstadio { get; init; }
    public required string Nombre { get; init; }
    public string? Ciudad { get; init; }
    public required string Pais { get; init; }
}