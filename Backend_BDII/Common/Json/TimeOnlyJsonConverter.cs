using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend_BDII.Common.Json;

public sealed class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    private const string WriteFormat = "HH:mm:ss";

    private static readonly string[] ReadFormats =
    [
        "HH:mm",
        "HH:mm:ss",
        "HH:mm:ss.FFFFFFF"
    ];

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        if (TimeOnly.TryParseExact(value, ReadFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            return time;

        throw new JsonException("La hora debe tener formato HH:mm o HH:mm:ss.");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(WriteFormat, CultureInfo.InvariantCulture));
    }
}
