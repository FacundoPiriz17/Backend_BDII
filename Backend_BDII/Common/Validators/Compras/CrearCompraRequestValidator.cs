using Backend_BDII.Modules.Compras.DTOs;
using FluentValidation;

namespace Backend_BDII.Common.Validators.Compras;

public sealed class CrearCompraRequestValidator : AbstractValidator<CrearCompraRequest>
{
    private static readonly HashSet<string> SectoresValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "A",
        "B",
        "C",
        "D"
    };

    public CrearCompraRequestValidator()
    {
        RuleFor(x => x.Entradas)
            .NotEmpty().WithMessage("Debe incluir al menos una entrada.")
            .Must(entradas => entradas is not null && entradas.Sum(e => e.Cantidad) <= 5)
            .WithMessage("No se pueden comprar mas de 5 entradas en la misma transaccion.");

        RuleForEach(x => x.Entradas).ChildRules(entrada =>
        {
            entrada.RuleFor(x => x.IdPartido)
                .GreaterThan(0).WithMessage("El partido es obligatorio.");

            entrada.RuleFor(x => x.NombreSector)
                .NotEmpty().WithMessage("El sector es obligatorio.")
                .Must(sector => !string.IsNullOrWhiteSpace(sector) && SectoresValidos.Contains(sector.Trim()))
                .WithMessage("El sector debe ser A, B, C o D.");

            entrada.RuleFor(x => x.Cantidad)
                .InclusiveBetween(1, 5).WithMessage("La cantidad debe estar entre 1 y 5.");
        });
    }
}