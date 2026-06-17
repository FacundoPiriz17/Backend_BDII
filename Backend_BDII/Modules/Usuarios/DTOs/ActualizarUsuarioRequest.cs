namespace Backend_BDII.Modules.Usuarios.DTOs;

public sealed class ActualizarUsuarioRequest
{
    public required string Nombre { get; init; }
    public required bool Habilitado { get; init; }

    public required string PaisDocumento { get; init; }
    public required string TipoDocumento { get; init; }
    public required int NumeroDocumento { get; init; }

    public string? LocalidadDireccion { get; init; }
    public string? CalleDireccion { get; init; }
    public string? PaisDireccion { get; init; }
    public int? NumeroDireccion { get; init; }
    public int? CodigoPostalDireccion { get; init; }

    public List<string> Telefonos { get; init; } = [];
}
