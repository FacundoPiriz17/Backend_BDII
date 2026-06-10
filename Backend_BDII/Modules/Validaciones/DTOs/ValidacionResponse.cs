namespace Backend_BDII.Modules.Validaciones.DTOs;

public sealed class ValidacionResponse
{
    public required int IdValidacion { get; init; }
    public required int IdEntrada { get; init; }
    public required int IdDispositivo { get; init; }
    public required string Estado { get; init; }
    public required string CodigoEscaneado { get; init; }
    public required DateTime FechaHora { get; init; }
    public required FuncionarioValidacionResponse Funcionario { get; init; }
    public required EntradaValidacionResponse Entrada { get; init; }
}

public sealed class FuncionarioValidacionResponse
{
    public required string Email { get; init; }
    public required string Nombre { get; init; }
    public required int NumeroLegajo { get; init; }
}

public sealed class EntradaValidacionResponse
{
    public required int IdEntrada { get; init; }
    public required string Estado { get; init; }
    public required int CostoTotal { get; init; }
    public required int TransferenciasRestantes { get; init; }
    public required string NombreSector { get; init; }
    public required string EmailPropietarioActual { get; init; }
    public required string NombrePropietarioActual { get; init; }
    public required string PaisDocumento { get; init; }
    public required string TipoDocumento { get; init; }
    public required int NumeroDocumento { get; init; }
    public required PartidoValidacionResponse Partido { get; init; }
}

public sealed class PartidoValidacionResponse
{
    public required int IdPartido { get; init; }
    public required DateOnly Fecha { get; init; }
    public required TimeOnly Hora { get; init; }
    public required string EquipoLocal { get; init; }
    public required string EquipoVisitante { get; init; }
    public required string Fase { get; init; }
    public required string Estado { get; init; }
    public required EstadioValidacionResponse Estadio { get; init; }
}

public sealed class EstadioValidacionResponse
{
    public required int IdEstadio { get; init; }
    public required string Nombre { get; init; }
    public string? Ciudad { get; init; }
    public required string Pais { get; init; }
}
