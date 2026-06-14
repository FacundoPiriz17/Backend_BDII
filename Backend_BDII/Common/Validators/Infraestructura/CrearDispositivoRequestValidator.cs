using Backend_BDII.Modules.Infraestructura.DTOs;
using FluentValidation;

namespace Backend_BDII.Common.Validators.Infraestructura;

public sealed class CrearDispositivoRequestValidator : AbstractValidator<CrearDispositivoRequest>
{
    public CrearDispositivoRequestValidator()
    {
        RuleFor(x => x.EmailFuncionario)
            .NotEmpty().WithMessage("El email del funcionario es obligatorio.")
            .EmailAddress().WithMessage("El email del funcionario no tiene un formato valido.");

        RuleFor(x => x.Modelo)
            .MaximumLength(30).WithMessage("El modelo no puede superar los 30 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Modelo));
    }
}
