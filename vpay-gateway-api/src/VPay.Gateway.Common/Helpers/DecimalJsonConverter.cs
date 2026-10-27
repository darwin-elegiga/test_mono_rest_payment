using System.Globalization;
using Newtonsoft.Json;

namespace VPay.Gateway.Common.Helpers;
public class DecimalJsonConverter : JsonConverter<decimal>
{
    public override void WriteJson(JsonWriter writer, decimal value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString("F2", CultureInfo.InvariantCulture));
    }

    public override decimal ReadJson(JsonReader reader, Type objectType, decimal existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return 0m;
        }

        if (reader.TokenType == JsonToken.Float || reader.TokenType == JsonToken.Integer)
        {
            return Convert.ToDecimal(reader.Value, CultureInfo.InvariantCulture);
        }

        if (reader.TokenType == JsonToken.String &&
            decimal.TryParse(reader.Value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        throw new JsonSerializationException($"Invalid decimal value at path '{reader.Path}'.");
    }
}
