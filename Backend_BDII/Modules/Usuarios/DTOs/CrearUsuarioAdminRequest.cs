namespace Backend_BDII.Modules.Usuarios.DTOs;

public sealed class CrearUsuarioAdminRequest
{
    public required string Email { get; init; }
    public required string Nombre { get; init; }
    public required string Password { get; init; }
    public bool Habilitado { get; init; } = true;

    public required string PaisDocumento { get; init; }
    public required string TipoDocumento { get; init; }
    public required int NumeroDocumento { get; init; }

    public string? LocalidadDireccion { get; init; }
    public string? CalleDireccion { get; init; }
    public string? PaisDireccion { get; init; }
    public int? NumeroDireccion { get; init; }
    public int? CodigoPostalDireccion { get; init; }

    public List<string> Telefonos { get; init; } = [];
    public required List<string> Roles { get; init; }

    public string? PaisAdmin { get; init; }
    public int? NumeroLegajo { get; init; }
    public bool? EstadoVerificacion { get; init; }
}
