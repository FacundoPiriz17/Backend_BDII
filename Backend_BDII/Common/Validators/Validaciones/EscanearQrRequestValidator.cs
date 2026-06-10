using Backend_BDII.Modules.Validaciones.DTOs;
using FluentValidation;

namespace Backend_BDII.Common.Validators.Validaciones;

public sealed class EscanearQrRequestValidator : AbstractValidator<EscanearQrRequest>
{
    public EscanearQrRequestValidator()
    {
        RuleFor(x => x.IdDispositivo)
            .GreaterThan(0).WithMessage("El dispositivo es obligatorio.");

        RuleFor(x => x.CodigoEscaneado)
            .NotEmpty().WithMessage("El codigo escaneado es obligatorio.")
            .MaximumLength(300).WithMessage("El codigo escaneado no puede superar los 300 caracteres.");
    }
}
