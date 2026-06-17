namespace Backend_BDII.Modules.Usuarios.DTOs;

public sealed class ActualizarRolesUsuarioRequest
{
    public required List<string> Roles { get; init; }
    public string? PaisAdmin { get; init; }
    public int? NumeroLegajo { get; init; }
    public bool? EstadoVerificacion { get; init; }
}
