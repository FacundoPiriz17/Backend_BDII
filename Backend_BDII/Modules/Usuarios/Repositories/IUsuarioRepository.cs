using Backend_BDII.Modules.Usuarios.DTOs;

namespace Backend_BDII.Modules.Usuarios.Repositories;

public interface IUsuarioRepository
{
    Task<List<UsuarioResponse>> GetAllAsync(string? rol, bool? habilitado, string? busqueda, CancellationToken cancellationToken = default);
    Task<UsuarioResponse?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<MiPerfilResponse?> GetMiPerfilAsync(string email, CancellationToken cancellationToken = default);
    Task<UsuarioResponse> CrearAsync(CrearUsuarioAdminRequest request, string passwordHash, CancellationToken cancellationToken = default);
    Task<UsuarioResponse?> ActualizarAsync(string email, ActualizarUsuarioRequest request, CancellationToken cancellationToken = default);
    Task<UsuarioResponse?> ActualizarHabilitacionAsync(string email, bool habilitado, CancellationToken cancellationToken = default);
    Task<UsuarioResponse?> ActualizarRolesAsync(string email, ActualizarRolesUsuarioRequest request, CancellationToken cancellationToken = default);
}
