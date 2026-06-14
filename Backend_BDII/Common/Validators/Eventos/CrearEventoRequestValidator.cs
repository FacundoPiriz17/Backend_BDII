using Backend_BDII.Modules.Eventos.DTOs;
using FluentValidation;

namespace Backend_BDII.Common.Validators.Eventos;

public sealed class CrearEventoRequestValidator : AbstractValidator<CrearEventoRequest>
{
    public CrearEventoRequestValidator()
    {
        RuleFor(x => x.IdEstadio)
            .GreaterThan(0).WithMessage("El estadio es obligatorio.");

        RuleFor(x => x.EquipoLocal)
            .NotEmpty().WithMessage("El equipo local es obligatorio.")
            .Length(3).WithMessage("El codigo FIFA del equipo local debe tener 3 caracteres.");

        RuleFor(x => x.EquipoVisitante)
            .NotEmpty().WithMessage("El equipo visitante es obligatorio.")
            .Length(3).WithMessage("El codigo FIFA del equipo visitante debe tener 3 caracteres.");

        RuleFor(x => x.Costo)
            .GreaterThanOrEqualTo(0).WithMessage("El costo base no puede ser negativo.");

        RuleFor(x => x.Fase)
            .NotEmpty().WithMessage("La fase es obligatoria.");

        RuleFor(x => x.SectoresHabilitados)
            .NotEmpty().WithMessage("Debe habilitar al menos un sector.");
    }
}
