using Backend_BDII.Modules.Usuarios.DTOs;
using Backend_BDII.Modules.Usuarios.Repositories;

namespace Backend_BDII.Modules.Usuarios.Services;

public sealed class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;

    public UsuarioService(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public Task<List<UsuarioResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _usuarioRepository.GetAllAsync(cancellationToken);
    }

    public Task<UsuarioResponse?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _usuarioRepository.GetByEmailAsync(email, cancellationToken);
    }
    
    public Task<MiPerfilResponse?> GetMiPerfilAsync(string email, CancellationToken cancellationToken = default)
    {
        return _usuarioRepository.GetMiPerfilAsync(email, cancellationToken);
    }
}