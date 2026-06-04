using System.Text.Json;
using System.Text.Json.Serialization;
using SpotRent.Domain.Enums;

namespace SpotRent.Api.Json;

public sealed class SpaceTypeJsonConverter : JsonConverter<SpaceType>
{
    public override SpaceType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var numericValue))
        {
            return (SpaceType)numericValue;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var rawValue = reader.GetString();

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return SpaceType.None;
            }

            if (int.TryParse(rawValue, out var numericStringValue))
            {
                return (SpaceType)numericStringValue;
            }

            if (Enum.TryParse<SpaceType>(rawValue, true, out var enumValue))
            {
                return enumValue;
            }
        }

        throw new JsonException($"Unable to convert value to {nameof(SpaceType)}.");
    }

    public override void Write(Utf8JsonWriter writer, SpaceType value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue((int)value);
    }
}
