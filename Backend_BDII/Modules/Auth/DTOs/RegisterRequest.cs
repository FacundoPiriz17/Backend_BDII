namespace Backend_BDII.Modules.Auth.DTOs;

public sealed class RegisterRequest
{
    public required string Email { get; init; }
    public required string Nombre { get; init; }
    public required string Password { get; init; }

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