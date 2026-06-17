namespace Backend_BDII.Modules.Auth.DTOs;

public sealed class AuthUser
{
    public required string Email { get; init; }
    public required string Nombre { get; init; }
    public required bool Habilitado { get; init; }
    public required string PasswordHash { get; init; }

    public bool EsGeneral { get; init; }
    public bool EsAdmin { get; init; }
    public bool EsFuncionario { get; init; }

    public List<string> GetRoles()
    {
        var roles = new List<string>();

        if (EsGeneral) roles.Add("General");
        if (EsAdmin) roles.Add("Admin");
        if (EsFuncionario) roles.Add("Funcionario");

        return roles;
    }
}