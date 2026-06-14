using Backend_BDII.Modules.Eventos.DTOs;
using FluentValidation;

namespace Backend_BDII.Common.Validators.Eventos;

public sealed class ActualizarEventoRequestValidator : AbstractValidator<ActualizarEventoRequest>
{
    public ActualizarEventoRequestValidator()
    {
        RuleFor(x => x.IdEstadio)
            .GreaterThan(0).WithMessage("El estadio es obligatorio.");

        RuleFor(x => x.EquipoLocal)
            .NotEmpty().WithMessage("El equipo local es obligatorio.")
            .Length(3).WithMessage("El codigo FIFA del equipo local debe tener 3 caracteres.");

        RuleFor(x => x.EquipoVisitante)
            .NotEmpty().WithMessage("El equipo visitante es obligatorio.")
            .Length(3).WithMessage("El codigo FIFA del equipo visitante debe tener 3 caracteres.");

        RuleFor(x => x.MarcadorLocal)
            .GreaterThanOrEqualTo(0).WithMessage("El marcador local no puede ser negativo.");

        RuleFor(x => x.MarcadorVisitante)
            .GreaterThanOrEqualTo(0).WithMessage("El marcador visitante no puede ser negativo.");

        RuleFor(x => x.Costo)
            .GreaterThanOrEqualTo(0).WithMessage("El costo base no puede ser negativo.");

        RuleFor(x => x.Fase)
            .NotEmpty().WithMessage("La fase es obligatoria.");

        RuleFor(x => x.Estado)
            .NotEmpty().WithMessage("El estado es obligatorio.");

        RuleFor(x => x.SectoresHabilitados)
            .NotEmpty().WithMessage("Debe habilitar al menos un sector.");
    }
}
