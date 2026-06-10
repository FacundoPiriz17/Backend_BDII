using Backend_BDII.Common.Auditing;
using Backend_BDII.Modules.Transferencias.DTOs;
using Backend_BDII.Modules.Transferencias.Repositories;

namespace Backend_BDII.Modules.Transferencias.Services;

public sealed class TransferenciaService : ITransferenciaService
{
    private static readonly HashSet<string> RelacionesValidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "todas",
        "enviadas",
        "recibidas"
    };

    private static readonly HashSet<string> EstadosValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "pendiente",
        "aceptada",
        "rechazada",
        "cancelada"
    };

    private readonly ITransferenciaRepository _transferenciaRepository;
    private readonly IAuditService _auditService;

    public TransferenciaService(ITransferenciaRepository transferenciaRepository, IAuditService auditService)
    {
        _transferenciaRepository = transferenciaRepository;
        _auditService = auditService;
    }

    public async Task<TransferenciaResponse> CrearAsync(
        string emailOrigen,
        CrearTransferenciaRequest request,
        CancellationToken cancellationToken = default)
    {
        var origen = NormalizeEmail(emailOrigen);

        if (request.IdEntrada <= 0)
            throw new InvalidOperationException("La entrada es obligatoria.");

        if (string.IsNullOrWhiteSpace(request.EmailDestino))
            throw new InvalidOperationException("El email destino es obligatorio.");

        var destino = NormalizeEmail(request.EmailDestino);

        if (origen == destino)
            throw new InvalidOperationException("No se puede transferir una entrada al mismo usuario.");

        if (await _transferenciaRepository.TieneTransferenciaPendienteAsync(request.IdEntrada, cancellationToken))
            throw new InvalidOperationException("La entrada ya tiene una transferencia pendiente.");

        var transferencia = await _transferenciaRepository.CrearAsync(origen, destino, request.IdEntrada, cancellationToken);

        _auditService.Record("transferencia.crear", origen, new
        {
            transferencia.IdTransferencia,
            transferencia.Entrada.IdEntrada,
            transferencia.EmailDestino
        });

        return transferencia;
    }

    public Task<List<TransferenciaResponse>> GetMisTransferenciasAsync(
        string emailUsuario,
        string? relacion,
        string? estado,
        int? idEntrada,
        string? busqueda,
        CancellationToken cancellationToken = default)
    {
        var relacionNormalizada = string.IsNullOrWhiteSpace(relacion)
            ? "todas"
            : relacion.Trim().ToLowerInvariant();

        if (!RelacionesValidas.Contains(relacionNormalizada))
            throw new InvalidOperationException("La relacion debe ser todas, enviadas o recibidas.");

        var estadoNormalizado = string.IsNullOrWhiteSpace(estado)
            ? null
            : estado.Trim().ToLowerInvariant();

        if (estadoNormalizado is not null && !EstadosValidos.Contains(estadoNormalizado))
            throw new InvalidOperationException("El estado debe ser pendiente, aceptada, rechazada o cancelada.");

        return _transferenciaRepository.GetByUsuarioAsync(
            NormalizeEmail(emailUsuario),
            relacionNormalizada,
            estadoNormalizado,
            idEntrada,
            busqueda,
            cancellationToken);
    }

    public Task<TransferenciaResponse?> GetByIdAsync(
        int idTransferencia,
        string emailUsuario,
        CancellationToken cancellationToken = default)
    {
        return _transferenciaRepository.GetByIdAsync(idTransferencia, NormalizeEmail(emailUsuario), cancellationToken);
    }

    public Task<TransferenciaResponse> AceptarAsync(
        int idTransferencia,
        string emailUsuario,
        CancellationToken cancellationToken = default)
    {
        return CambiarEstadoAsync(idTransferencia, emailUsuario, "destino", "aceptada", cancellationToken);
    }

    public Task<TransferenciaResponse> RechazarAsync(
        int idTransferencia,
        string emailUsuario,
        CancellationToken cancellationToken = default)
    {
        return CambiarEstadoAsync(idTransferencia, emailUsuario, "destino", "rechazada", cancellationToken);
    }

    public Task<TransferenciaResponse> CancelarAsync(
        int idTransferencia,
        string emailUsuario,
        CancellationToken cancellationToken = default)
    {
        return CambiarEstadoAsync(idTransferencia, emailUsuario, "origen", "cancelada", cancellationToken);
    }

    private async Task<TransferenciaResponse> CambiarEstadoAsync(
        int idTransferencia,
        string emailUsuario,
        string rolUsuario,
        string nuevoEstado,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(emailUsuario);
        var transferencia = await _transferenciaRepository.GetByIdAsync(idTransferencia, email, cancellationToken)
                            ?? throw new KeyNotFoundException("Transferencia no encontrada.");

        if (transferencia.Estado != "pendiente")
            throw new InvalidOperationException("Solo se pueden modificar transferencias pendientes.");

        var esDestino = string.Equals(transferencia.EmailDestino, email, StringComparison.OrdinalIgnoreCase);
        var esOrigen = string.Equals(transferencia.EmailOrigen, email, StringComparison.OrdinalIgnoreCase);

        if (rolUsuario == "destino" && !esDestino)
            throw new UnauthorizedAccessException("Solo el destinatario puede aceptar o rechazar la transferencia.");

        if (rolUsuario == "origen" && !esOrigen)
            throw new UnauthorizedAccessException("Solo el origen puede cancelar la transferencia.");

        var transferenciaActualizada = await _transferenciaRepository.ActualizarEstadoAsync(
                   idTransferencia,
                   email,
                   rolUsuario,
                   nuevoEstado,
                   cancellationToken)
               ?? throw new InvalidOperationException("No se pudo actualizar la transferencia.");

        _auditService.Record($"transferencia.{nuevoEstado}", email, new
        {
            transferenciaActualizada.IdTransferencia,
            transferenciaActualizada.Entrada.IdEntrada
        });

        return transferenciaActualizada;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
