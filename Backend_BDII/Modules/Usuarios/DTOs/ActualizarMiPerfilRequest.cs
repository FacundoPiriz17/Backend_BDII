namespace Backend_BDII.Modules.Usuarios.DTOs;

/// <summary>
/// Datos que cualquier usuario autenticado puede editar de su propio perfil.
/// No incluye documento, habilitacion ni roles: esos campos solo los puede
/// tocar un administrador desde el modulo de gestion de usuarios.
/// </summary>
public sealed class ActualizarMiPerfilRequest
{
    public required string Nombre { get; init; }

    public string? LocalidadDireccion { get; init; }
    public string? CalleDireccion { get; init; }
    public string? PaisDireccion { get; init; }
    public int? NumeroDireccion { get; init; }
    public int? CodigoPostalDireccion { get; init; }

    public List<string> Telefonos { get; init; } = [];
}
