using System.Text.RegularExpressions;
using Backend_BDII.Common.Auditing;
using Backend_BDII.Modules.Validaciones.DTOs;
using Backend_BDII.Modules.Validaciones.Repositories;

namespace Backend_BDII.Modules.Validaciones.Services;

public sealed partial class ValidacionService : IValidacionService
{
    private const string EstadoValida = "v\u00e1lida";
    private const string EstadoInvalida = "inv\u00e1lida";

    private readonly IValidacionRepository _validacionRepository;
    private readonly IAuditService _auditService;

    public ValidacionService(IValidacionRepository validacionRepository, IAuditService auditService)
    {
        _validacionRepository = validacionRepository;
        _auditService = auditService;
    }

    public async Task<ValidacionResponse> EscanearQrAsync(
        string emailFuncionario,
        EscanearQrRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.IdDispositivo <= 0)
            throw new InvalidOperationException("El dispositivo es obligatorio.");
        
        if (string.IsNullOrWhiteSpace(request.CodigoEscaneado))
            throw new InvalidOperationException("El codigo escaneado es obligatorio.");

        var idEntrada = ExtraerIdEntrada(request.CodigoEscaneado);
        var codigoQrActual = await _validacionRepository.GetCodigoQrActualAsync(idEntrada, cancellationToken);
        var estado = string.Equals(codigoQrActual, request.CodigoEscaneado, StringComparison.Ordinal)
            ? EstadoValida
            : EstadoInvalida;

        var email = NormalizeEmail(emailFuncionario);
        
        var validacion = await _validacionRepository.RegistrarAsync(
            email,
            request.IdDispositivo,
            idEntrada,
            request.CodigoEscaneado,
            estado,
            cancellationToken);

        _auditService.Record("validacion.escanear_qr", email, new
        {
            request.IdDispositivo,
            IdEntrada = idEntrada,
            Estado = estado
        });

        return validacion;
    }

    public async Task<ValidacionResponse> RegistrarInvalidacionAsync(
        string emailFuncionario,
        RegistrarInvalidacionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.IdEntrada <= 0)
            throw new InvalidOperationException("La entrada es obligatoria.");

        if (string.IsNullOrWhiteSpace(request.CodigoEscaneado))
            throw new InvalidOperationException("El codigo escaneado es obligatorio.");

        var email = NormalizeEmail(emailFuncionario);
        var validacion = await _validacionRepository.RegistrarAsync(
            email,
            request.IdDispositivo,
            request.IdEntrada,
            request.CodigoEscaneado,
            EstadoInvalida,
            cancellationToken);

        _auditService.Record("validacion.invalidar", email, new
        {
            request.IdDispositivo,
            request.IdEntrada
        });

        return validacion;
    }

    public Task<List<ValidacionResponse>> GetHistorialAsync(
        string? emailFuncionario,
        int? idPartido,
        int? idEntrada,
        string? estado,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 200);
        var normalizedEstado = NormalizeEstado(estado);

        return _validacionRepository.GetHistorialAsync(
            string.IsNullOrWhiteSpace(emailFuncionario) ? null : NormalizeEmail(emailFuncionario),
            idPartido,
            idEntrada,
            normalizedEstado,
            normalizedLimit,
            cancellationToken);
    }

    public Task<ValidacionResponse?> GetByIdAsync(int idValidacion, CancellationToken cancellationToken = default)
    {
        return _validacionRepository.GetByIdAsync(idValidacion, cancellationToken);
    }

    public async Task<VerificacionManualResponse> VerificarEntradaManualAsync(
        VerificarEntradaManualRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.IdEntrada <= 0)
            throw new InvalidOperationException("La entrada es obligatoria.");

        if (request.NumeroDocumento <= 0)
            throw new InvalidOperationException("El numero de documento debe ser mayor a 0.");

        return await _validacionRepository.VerificarEntradaManualAsync(
                   request.IdEntrada,
                   request.NumeroDocumento,
                   cancellationToken)
               ?? throw new KeyNotFoundException("Entrada no encontrada.");
    }

    private static int ExtraerIdEntrada(string codigoEscaneado)
    {
        var match = QrEntradaRegex().Match(codigoEscaneado);

        if (!match.Success || !int.TryParse(match.Groups["id"].Value, out var idEntrada))
            throw new InvalidOperationException("El QR no tiene un identificador de entrada valido.");

        return idEntrada;
    }

    private static string? NormalizeEstado(string? estado)
    {
        if (string.IsNullOrWhiteSpace(estado))
            return null;

        return estado.Trim().ToLowerInvariant() switch
        {
            "valida" or "v\u00e1lida" => EstadoValida,
            "invalida" or "inv\u00e1lida" => EstadoInvalida,
            _ => throw new InvalidOperationException("El estado debe ser valida o invalida.")
        };
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    [GeneratedRegex(@"entrada:(?<id>\d+)\|", RegexOptions.IgnoreCase)]
    private static partial Regex QrEntradaRegex();
}
