namespace Backend_BDII.Modules.Auth.DTOs;

public sealed class AuthResponse
{
    public required string Token { get; init; }
    public required string Email { get; init; }
    public required string Nombre { get; init; }
    public required List<string> Roles { get; init; }
}