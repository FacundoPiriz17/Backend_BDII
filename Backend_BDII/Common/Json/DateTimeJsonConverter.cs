using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend_BDII.Common.Json;

public sealed class DateTimeJsonConverter : JsonConverter<DateTime>
{
    private const string LocalFormat = "yyyy-MM-dd'T'HH:mm:ss";
    private const string UtcFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTime))
            return dateTime;

        throw new JsonException("La fecha y hora debe tener formato ISO 8601.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var format = value.Kind == DateTimeKind.Utc ? UtcFormat : LocalFormat;
        writer.WriteStringValue(value.ToString(format, CultureInfo.InvariantCulture));
    }
}
