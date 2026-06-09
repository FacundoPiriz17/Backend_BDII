using Backend_BDII.Modules.Usuarios.DTOs;

namespace Backend_BDII.Modules.Usuarios.Repositories;

public interface IUsuarioRepository
{
    Task<List<UsuarioResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UsuarioResponse?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    
    Task<MiPerfilResponse?> GetMiPerfilAsync(string email, CancellationToken cancellationToken = default);
}