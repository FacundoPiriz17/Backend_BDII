using Backend_BDII.Modules.Eventos.DTOs;
using FluentValidation;

namespace Backend_BDII.Common.Validators.Eventos;

public sealed class CambiarEstadoEventoRequestValidator : AbstractValidator<CambiarEstadoEventoRequest>
{
    public CambiarEstadoEventoRequestValidator()
    {
        RuleFor(x => x.Estado)
            .NotEmpty().WithMessage("El estado es obligatorio.");
    }
}
