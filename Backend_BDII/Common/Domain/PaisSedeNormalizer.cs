using System.Globalization;
using System.Text;

namespace Backend_BDII.Common.Domain;
//Esta clase es un normalizador el cual facilita la experiencia de Usuario transformando ciertos nombres o caracteres en otros que sean útiles
public static class PaisSedeNormalizer
{
    public static string Normalize(string pais)
    {
        var trimmed = pais.Trim();
        var comparable = RemoveDiacritics(trimmed).ToLowerInvariant();

        return comparable switch
        {
            "mexico" => "México",
            "canada" => "Canadá",
            "eeuu" or "usa" or "estados unidos" or "estados unidos de america" => "EEUU",
            _ => trimmed
        };
    }
//Este metodo elimina tildes, diéresis, entre otras cosas
    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(capacity: normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
