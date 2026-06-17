using Backend_BDII.Modules.Infraestructura.DTOs;
using FluentValidation;

namespace Backend_BDII.Common.Validators.Infraestructura;

public sealed class CrearEstadioRequestValidator : AbstractValidator<CrearEstadioRequest>
{
    public CrearEstadioRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del estadio es obligatorio.")
            .MinimumLength(3).WithMessage("El nombre del estadio debe tener al menos 3 caracteres.")
            .MaximumLength(32).WithMessage("El nombre del estadio no puede superar los 32 caracteres.");

        RuleFor(x => x.Capacidad)
            .GreaterThan(0).WithMessage("La capacidad debe ser mayor a 0.")
            .When(x => x.Capacidad.HasValue);

        RuleFor(x => x.Pais)
            .NotEmpty().WithMessage("El pais es obligatorio.");

        RuleFor(x => x.Sectores)
            .NotEmpty().WithMessage("Debe crear al menos un sector.");
    }
}
