using Backend_BDII.Modules.Validaciones.DTOs;

namespace Backend_BDII.Modules.Validaciones.Repositories;

public interface IValidacionRepository
{
    Task<ValidacionResponse> RegistrarAsync(
        string emailFuncionario,
        int idDispositivo,
        int idEntrada,
        string codigoEscaneado,
        string estado,
        CancellationToken cancellationToken = default);

    Task<string?> GetCodigoQrActualAsync(int idEntrada, CancellationToken cancellationToken = default);

    Task<List<ValidacionResponse>> GetHistorialAsync(
        string? emailFuncionario,
        int? idPartido,
        int? idEntrada,
        string? estado,
        int limit,
        CancellationToken cancellationToken = default);

    Task<ValidacionResponse?> GetByIdAsync(int idValidacion, CancellationToken cancellationToken = default);
    Task<VerificacionManualResponse?> VerificarEntradaManualAsync(int idEntrada, int numeroDocumento, CancellationToken cancellationToken = default);
}
