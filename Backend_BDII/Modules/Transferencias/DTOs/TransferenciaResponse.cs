namespace Backend_BDII.Modules.Transferencias.DTOs;

public sealed class TransferenciaResponse
{
    public required int IdTransferencia { get; init; }
    public required DateTime FechaHora { get; init; }
    public required string EmailOrigen { get; init; }
    public required string EmailDestino { get; init; }
    public required string Estado { get; init; }
    public required EntradaTransferenciaResponse Entrada { get; init; }
}

public sealed class EntradaTransferenciaResponse
{
    public required int IdEntrada { get; init; }
    public required string Estado { get; init; }
    public required int CostoTotal { get; init; }
    public required int TransferenciasRestantes { get; init; }
    public required string NombreSector { get; init; }
    public required string EmailPropietarioActual { get; init; }
    public required PartidoTransferenciaResponse Partido { get; init; }
}

public sealed class PartidoTransferenciaResponse
{
    public required int IdPartido { get; init; }
    public required DateOnly Fecha { get; init; }
    public required TimeOnly Hora { get; init; }
    public required string EquipoLocal { get; init; }
    public required string EquipoVisitante { get; init; }
    public required string Fase { get; init; }
    public required string Estado { get; init; }
    public required EstadioTransferenciaResponse Estadio { get; init; }
}

public sealed class EstadioTransferenciaResponse
{
    public required int IdEstadio { get; init; }
    public required string Nombre { get; init; }
    public string? Ciudad { get; init; }
    public required string Pais { get; init; }
}
