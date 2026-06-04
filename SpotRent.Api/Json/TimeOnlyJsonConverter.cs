using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpotRent.Api.Json;

public sealed class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    private static readonly string[] AcceptedFormats =
    [
        "HH:mm",
        "H:mm",
        "HH:mm:ss",
        "H:mm:ss",
        "HH:mm:ss.fff",
        "HH:mm:ss.fffffff"
    ];

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Time value must be a string.");
        }

        var rawValue = reader.GetString();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            throw new JsonException("Time value cannot be empty.");
        }

        if (TimeOnly.TryParseExact(rawValue, AcceptedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var parsedExact))
        {
            return parsedExact;
        }

        if (TimeOnly.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        throw new JsonException("Invalid time format. Expected HH:mm or HH:mm:ss.");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
    }
}
