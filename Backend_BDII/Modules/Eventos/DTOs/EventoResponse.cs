namespace Backend_BDII.Modules.Eventos.DTOs;

public sealed class EventoResponse
{
    public required int IdPartido { get; init; }
    public required DateOnly Fecha { get; init; }
    public required TimeOnly Hora { get; init; }
    public required string EquipoLocal { get; init; }
    public required string EquipoVisitante { get; init; }
    public required int MarcadorLocal { get; init; }
    public required int MarcadorVisitante { get; init; }
    public required int CostoBase { get; init; }
    public required string Fase { get; init; }
    public required string Estado { get; init; }
    public required string EmailAdmin { get; init; }
    public required DateOnly FechaHabilitacion { get; init; }
    public required EstadioEventoResponse Estadio { get; init; }
    public required List<SectorEventoResponse> Sectores { get; init; }
}

public sealed class EstadioEventoResponse
{
    public required int IdEstadio { get; init; }
    public required string Nombre { get; init; }
    public int? Capacidad { get; init; }
    public string? Ubicacion { get; init; }
    public string? Ciudad { get; init; }
    public required string Pais { get; init; }
}

public sealed class SectorEventoResponse
{
    public required string NombreSector { get; init; }
    public required int Capacidad { get; init; }
    public required int CostoSector { get; init; }
    public required int CostoTotalEntrada { get; init; }
    public required int EntradasVendidas { get; init; }
    public required int EntradasDisponibles { get; init; }
}
