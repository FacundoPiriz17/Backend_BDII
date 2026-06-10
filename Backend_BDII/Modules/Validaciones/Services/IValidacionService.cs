using Backend_BDII.Modules.Validaciones.DTOs;

namespace Backend_BDII.Modules.Validaciones.Services;

public interface IValidacionService
{
    Task<ValidacionResponse> EscanearQrAsync(string emailFuncionario, EscanearQrRequest request, CancellationToken cancellationToken = default);
    Task<ValidacionResponse> RegistrarInvalidacionAsync(string emailFuncionario, RegistrarInvalidacionRequest request, CancellationToken cancellationToken = default);
    Task<List<ValidacionResponse>> GetHistorialAsync(string? emailFuncionario, int? idPartido, int? idEntrada, string? estado, int limit, CancellationToken cancellationToken = default);
    Task<ValidacionResponse?> GetByIdAsync(int idValidacion, CancellationToken cancellationToken = default);
    Task<VerificacionManualResponse> VerificarEntradaManualAsync(VerificarEntradaManualRequest request, CancellationToken cancellationToken = default);
}
