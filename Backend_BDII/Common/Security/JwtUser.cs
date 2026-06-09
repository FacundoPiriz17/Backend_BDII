namespace Backend_BDII.Common.Security;

public sealed class JwtUser
{
    public required string Email { get; init; }
    public required string Nombre { get; init; }
    public required List<string> Roles { get; init; }
}