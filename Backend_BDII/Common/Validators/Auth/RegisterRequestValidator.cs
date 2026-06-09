using Backend_BDII.Modules.Auth.DTOs;
using FluentValidation;

namespace Backend_BDII.Common.Validators.Auth;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El email no tiene un formato válido.")
            .Must(SerEmailUcu).WithMessage("El email debe pertenecer al dominio @ucu.edu.uy o @correo.ucu.edu.uy.");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MinimumLength(3).WithMessage("El nombre debe tener al menos 3 caracteres.")
            .MaximumLength(32).WithMessage("El nombre no puede superar los 32 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");

        RuleFor(x => x.PaisDocumento)
            .NotEmpty().WithMessage("El país del documento es obligatorio.")
            .MaximumLength(32).WithMessage("El país del documento no puede superar los 32 caracteres.");

        RuleFor(x => x.TipoDocumento)
            .NotEmpty().WithMessage("El tipo de documento es obligatorio.")
            .MaximumLength(32).WithMessage("El tipo de documento no puede superar los 32 caracteres.");

        RuleFor(x => x.NumeroDocumento)
            .GreaterThan(0).WithMessage("El número de documento debe ser mayor a 0.");

        RuleFor(x => x.LocalidadDireccion)
            .MaximumLength(40).WithMessage("La localidad no puede superar los 40 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.LocalidadDireccion));

        RuleFor(x => x.CalleDireccion)
            .MaximumLength(50).WithMessage("La calle no puede superar los 50 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.CalleDireccion));

        RuleFor(x => x.PaisDireccion)
            .MaximumLength(32).WithMessage("El país de dirección no puede superar los 32 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.PaisDireccion));

        RuleFor(x => x.NumeroDireccion)
            .GreaterThan(0).WithMessage("El número de dirección debe ser mayor a 0.")
            .When(x => x.NumeroDireccion.HasValue);

        RuleFor(x => x.CodigoPostalDireccion)
            .GreaterThan(0).WithMessage("El código postal debe ser mayor a 0.")
            .When(x => x.CodigoPostalDireccion.HasValue);

        RuleForEach(x => x.Telefonos)
            .NotEmpty().WithMessage("Los teléfonos no pueden estar vacíos.")
            .MaximumLength(20).WithMessage("Cada teléfono no puede superar los 20 caracteres.");
    }

    private static bool SerEmailUcu(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return normalizedEmail.EndsWith("@ucu.edu.uy")
               || normalizedEmail.EndsWith("@correo.ucu.edu.uy");
    }
}