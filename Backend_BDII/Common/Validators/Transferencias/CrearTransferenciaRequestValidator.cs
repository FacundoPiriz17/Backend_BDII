using Backend_BDII.Modules.Transferencias.DTOs;
using FluentValidation;

namespace Backend_BDII.Common.Validators.Transferencias;

public sealed class CrearTransferenciaRequestValidator : AbstractValidator<CrearTransferenciaRequest>
{
    public CrearTransferenciaRequestValidator()
    {
        RuleFor(x => x.IdEntrada)
            .GreaterThan(0).WithMessage("La entrada es obligatoria.");

        RuleFor(x => x.EmailDestino)
            .NotEmpty().WithMessage("El email destino es obligatorio.")
            .EmailAddress().WithMessage("El email destino no tiene un formato valido.")
            .Must(SerEmailUcu).WithMessage("El email destino debe pertenecer al dominio @ucu.edu.uy o @correo.ucu.edu.uy.");
    }

    private static bool SerEmailUcu(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var normalizedEmail = email.Trim().ToLowerInvariant();

        return normalizedEmail.EndsWith("@ucu.edu.uy")
               || normalizedEmail.EndsWith("@correo.ucu.edu.uy");
    }
}