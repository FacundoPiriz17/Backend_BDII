using Backend_BDII.Modules.Validaciones.DTOs;
using FluentValidation;

namespace Backend_BDII.Common.Validators.Validaciones;

public sealed class VerificarEntradaManualRequestValidator : AbstractValidator<VerificarEntradaManualRequest>
{
    public VerificarEntradaManualRequestValidator()
    {
        RuleFor(x => x.IdEntrada)
            .GreaterThan(0).WithMessage("La entrada es obligatoria.");

        RuleFor(x => x.NumeroDocumento)
            .GreaterThan(0).WithMessage("El numero de documento debe ser mayor a 0.");
    }
}
