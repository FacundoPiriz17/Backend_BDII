using Backend_BDII.Modules.Infraestructura.DTOs;
using FluentValidation;

namespace Backend_BDII.Common.Validators.Infraestructura;

public sealed class ActualizarSectorRequestValidator : AbstractValidator<ActualizarSectorRequest>
{
    public ActualizarSectorRequestValidator()
    {
        RuleFor(x => x.Capacidad)
            .GreaterThan(0).WithMessage("La capacidad del sector debe ser mayor a 0.")
            .When(x => x.Capacidad.HasValue);

        RuleFor(x => x.Costo)
            .GreaterThanOrEqualTo(0).WithMessage("El costo del sector no puede ser negativo.")
            .When(x => x.Costo.HasValue);
    }
}
