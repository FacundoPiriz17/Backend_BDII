using Backend_BDII.Modules.Infraestructura.DTOs;
using Backend_BDII.Modules.Infraestructura.Repositories;

namespace Backend_BDII.Modules.Infraestructura.Services;

public sealed class InfraestructuraService : IInfraestructuraService
{
    private static readonly HashSet<string> SectoresValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "A",
        "B",
        "C",
        "D"
    };

    private readonly IInfraestructuraRepository _infraestructuraRepository;

    public InfraestructuraService(IInfraestructuraRepository infraestructuraRepository)
    {
        _infraestructuraRepository = infraestructuraRepository;
    }

    public Task<List<EstadioResponse>> GetEstadiosAsync(string? pais, CancellationToken cancellationToken = default)
    {
        return _infraestructuraRepository.GetEstadiosAsync(pais, cancellationToken);
    }

    public Task<EstadioResponse?> GetEstadioByIdAsync(int idEstadio, CancellationToken cancellationToken = default)
    {
        return _infraestructuraRepository.GetEstadioByIdAsync(idEstadio, cancellationToken);
    }

    public Task<EstadioResponse> CrearEstadioAsync(
        string emailAdmin,
        CrearEstadioRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidarCreacionEstadio(request.Nombre, request.Capacidad, request.Pais);

        return _infraestructuraRepository.CrearEstadioAsync(
            NormalizeEmail(emailAdmin),
            request,
            cancellationToken);
    }

    public async Task<EstadioResponse> ActualizarEstadioAsync(
        int idEstadio,
        string emailAdmin,
        ActualizarEstadioRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidarActualizacionEstadio(request.Nombre, request.Capacidad, request.Pais);

        return await _infraestructuraRepository.ActualizarEstadioAsync(
                   idEstadio,
                   NormalizeEmail(emailAdmin),
                   request,
                   cancellationToken)
               ?? throw new KeyNotFoundException("Estadio no encontrado.");
    }

    public async Task<SectorInfraestructuraResponse> ActualizarSectorAsync(
        int idEstadio,
        string nombreSector,
        string emailAdmin,
        ActualizarSectorRequest request,
        CancellationToken cancellationToken = default)
    {
        var sector = nombreSector.Trim().ToUpperInvariant();

        if (!SectoresValidos.Contains(sector))
            throw new InvalidOperationException("El sector debe ser A, B, C o D.");

        if (request.Capacidad.HasValue && request.Capacidad.Value <= 0)
            throw new InvalidOperationException("La capacidad del sector debe ser mayor a 0.");

        if (request.Costo.HasValue && request.Costo.Value < 0)
            throw new InvalidOperationException("El costo del sector no puede ser negativo.");

        return await _infraestructuraRepository.ActualizarSectorAsync(
                   idEstadio,
                   sector,
                   NormalizeEmail(emailAdmin),
                   request,
                   cancellationToken)
               ?? throw new KeyNotFoundException("Sector o estadio no encontrado.");
    }

    public Task<List<EquipoResponse>> GetEquiposAsync(CancellationToken cancellationToken = default)
    {
        return _infraestructuraRepository.GetEquiposAsync(cancellationToken);
    }

    public Task<List<DispositivoResponse>> GetDispositivosAsync(
        string? emailFuncionario,
        CancellationToken cancellationToken = default)
    {
        return _infraestructuraRepository.GetDispositivosAsync(
            string.IsNullOrWhiteSpace(emailFuncionario) ? null : NormalizeEmail(emailFuncionario),
            cancellationToken);
    }

    public Task<DispositivoResponse?> GetDispositivoByIdAsync(int idDispositivo, CancellationToken cancellationToken = default)
    {
        return _infraestructuraRepository.GetDispositivoByIdAsync(idDispositivo, cancellationToken);
    }

    public Task<DispositivoResponse> CrearDispositivoAsync(
        CrearDispositivoRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidarEmailFuncionario(request.EmailFuncionario);
        return _infraestructuraRepository.CrearDispositivoAsync(request, cancellationToken);
    }

    public async Task<DispositivoResponse> ActualizarDispositivoAsync(
        int idDispositivo,
        ActualizarDispositivoRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidarEmailFuncionario(request.EmailFuncionario);

        return await _infraestructuraRepository.ActualizarDispositivoAsync(idDispositivo, request, cancellationToken)
               ?? throw new KeyNotFoundException("Dispositivo no encontrado.");
    }

    private static void ValidarCreacionEstadio(string nombre, int capacidad, string pais)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length < 3)
            throw new InvalidOperationException("El nombre del estadio debe tener al menos 3 caracteres.");

        if (capacidad <= 0)
            throw new InvalidOperationException("La capacidad del estadio debe ser mayor a 0.");

        if (string.IsNullOrWhiteSpace(pais))
            throw new InvalidOperationException("El país del estadio es obligatorio.");
    }

    private static void ValidarActualizacionEstadio(string nombre, int? capacidad, string pais)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length < 3)
            throw new InvalidOperationException("El nombre del estadio debe tener al menos 3 caracteres.");

        if (capacidad.HasValue && capacidad.Value <= 0)
            throw new InvalidOperationException("La capacidad del estadio debe ser mayor a 0.");

        if (string.IsNullOrWhiteSpace(pais))
            throw new InvalidOperationException("El país del estadio es obligatorio.");
    }

    private static void ValidarEmailFuncionario(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("El email del funcionario es obligatorio.");
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
