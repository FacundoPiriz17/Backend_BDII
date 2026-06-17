namespace Backend_BDII.Modules.Usuarios.DTOs;

public sealed class UsuarioResponse
{
    public required string Email { get; init; }
    public required string Nombre { get; init; }
    public required bool Habilitado { get; init; }
    public required string PaisDocumento { get; init; }
    public required string TipoDocumento { get; init; }
    public required int NumeroDocumento { get; init; }
    public required List<string> Roles { get; init; }
    public string? PaisAdmin { get; init; }
    public int? NumeroLegajo { get; init; }
    public DateTime? FechaRegistroGeneral { get; init; }
    public bool? EstadoVerificacionGeneral { get; init; }
}
