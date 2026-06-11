using Backend_BDII.Modules.Entradas.DTOs;
using Backend_BDII.Modules.Entradas.Repositories;

namespace Backend_BDII.Modules.Entradas.Services;

public sealed class EntradaService : IEntradaService
{
    private readonly IEntradaRepository _entradaRepository;

    public EntradaService(IEntradaRepository entradaRepository)
    {
        _entradaRepository = entradaRepository;
    }

    public Task<List<EntradasResponse>> GetMisEntradasAsync(
        string emailUsuario,
        CancellationToken cancellationToken = default)
    {
        return _entradaRepository.GetByUsuarioAsync(
            NormalizeEmail(emailUsuario),
            cancellationToken);
    }

    public async Task<EntradasResponse?> GetByIdAsync(
        int idEntrada,
        string emailUsuario,
        CancellationToken cancellationToken = default)
    {
        return await _entradaRepository.GetByIdAsync(
            idEntrada,
            cancellationToken);
    }

    public async Task<EntradasResponse> ActualizarEstadoAsync(
        int idEntrada,
        string nuevoEstado,
        CancellationToken cancellationToken = default)
    {

        return await _entradaRepository.ActualizarEstadoAsync(
                   idEntrada,
                   nuevoEstado,
                   cancellationToken)
               ?? throw new KeyNotFoundException("Entrada no encontrada.");
    }

 
    private async Task<EntradasResponse> GetEntradaExistenteAsync(
        int idEntrada,
        string emailUsuario,
        CancellationToken cancellationToken)
    {
        return await _entradaRepository.GetByIdAsync(
                   idEntrada,
                   cancellationToken)
               ?? throw new KeyNotFoundException("Entrada no encontrada.");
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}