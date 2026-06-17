using Backend_BDII.Modules.Usuarios.DTOs;

namespace Backend_BDII.Modules.Usuarios.Services;

public interface IUsuarioService
{
    Task<List<UsuarioResponse>> GetAllAsync(string? rol, bool? habilitado, string? busqueda, CancellationToken cancellationToken = default);
    Task<UsuarioResponse?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<MiPerfilResponse?> GetMiPerfilAsync(string email, CancellationToken cancellationToken = default);
    Task<UsuarioResponse> CrearAsync(string emailAdmin, CrearUsuarioAdminRequest request, CancellationToken cancellationToken = default);
    Task<UsuarioResponse> ActualizarAsync(string emailAdmin, string email, ActualizarUsuarioRequest request, CancellationToken cancellationToken = default);
    Task<MiPerfilResponse> ActualizarMiPerfilAsync(string email, ActualizarMiPerfilRequest request, CancellationToken cancellationToken = default);
    Task<UsuarioResponse> ActualizarHabilitacionAsync(string emailAdmin, string email, bool habilitado, CancellationToken cancellationToken = default);
    Task<UsuarioResponse> ActualizarRolesAsync(string emailAdmin, string email, ActualizarRolesUsuarioRequest request, CancellationToken cancellationToken = default);
}
