using System;
using Newtonsoft.Json;

namespace Ardalis.SmartEnum.JsonNet
{
    /// <summary>
    ///     Ardalis.SmartEnum.JsonNet package is not working with current version so created this manually
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class SmartEnumNameConverter<TEnum, TValue> : JsonConverter<TEnum>
        where TEnum : SmartEnum<TEnum, TValue>
        where TValue : struct, IEquatable<TValue>, IComparable<TValue>
    {
        public override bool CanRead => true;

        public override bool CanWrite => true;

        public override TEnum ReadJson(JsonReader reader, Type objectType, TEnum existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            return reader.TokenType switch
            {
                JsonToken.String => GetFromName((string) reader.Value),
                _ => throw new JsonSerializationException(
                    $"Unexpected token {reader.TokenType} when parsing a smart enum.")
            };

            static TEnum GetFromName(string name)
            {
                try
                {
                    return SmartEnum<TEnum, TValue>.FromName(name);
                }
                catch (Exception ex)
                {
                    throw new JsonSerializationException($"Error converting value '{name}' to a smart enum.", ex);
                }
            }
        }

        public override void WriteJson(JsonWriter writer, TEnum value, JsonSerializer serializer)
        {
            if (value is null)
                writer.WriteNull();
            else
                writer.WriteValue(value.Name);
        }
    }
}