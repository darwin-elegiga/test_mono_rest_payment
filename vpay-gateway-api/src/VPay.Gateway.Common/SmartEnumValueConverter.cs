using System.Reflection;
using Ardalis.SmartEnum;
using Newtonsoft.Json;

namespace VPay.Gateway.Common;

public class SmartEnumValueConverter : JsonConverter
{
    public override bool CanRead => true;

    public override bool CanWrite => true;

    private Type SmartEnumType { get; } = typeof(SmartEnum<,>);

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteValue((int)value.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public).GetValue(value));
        }
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        try
        {
            var smartEnum = GetBaseType(SmartEnumType, objectType);
            var typeArguments = smartEnum.GetGenericArguments();
            var fromValueMethod = smartEnum.GetMethod(
                "FromValue",
                BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Static,
                Type.DefaultBinder,
                new[] { typeArguments[1] },
                null);

            if (reader.TokenType == JsonToken.Integer && typeArguments[1] != typeof(long) && typeArguments[1] != typeof(bool))
            {
                return fromValueMethod.Invoke(null, new[] { Convert.ChangeType(reader.Value, typeArguments[1]) });
            }

            return fromValueMethod.Invoke(null, new[] { reader.Value });
        }
        catch (Exception ex)
        {
            throw new JsonSerializationException($"Error converting {reader.Value ?? "Null"} to {objectType.Name}.", ex);
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return GetBaseType(SmartEnumType, objectType) != null;
    }

    private static Type GetBaseType(Type generic, Type toCheck)
    {
        while (toCheck != null && toCheck != typeof(object))
        {
            var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
            if (generic == cur)
            {
                return toCheck;
            }

            toCheck = toCheck.BaseType;
        }

        return null;
    }
}

public class SmartEnumValueConverter<TEnum, TValue> : JsonConverter<TEnum>
       where TEnum : SmartEnum<TEnum, TValue>
       where TValue : struct, IEquatable<TValue>, IComparable<TValue>
{
    public override bool CanRead => true;

    public override bool CanWrite => true;

    public override TEnum ReadJson(JsonReader reader, Type objectType, TEnum existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        try
        {
            TValue value;
            if (reader.TokenType == JsonToken.Integer && typeof(TValue) != typeof(long) && typeof(TValue) != typeof(bool))
            {
                value = (TValue)Convert.ChangeType(reader.Value, typeof(TValue));
            }
            else
            {
                value = (TValue)reader.Value;
            }

            return SmartEnum<TEnum, TValue>.FromValue(value);
        }
        catch (Exception ex)
        {
            throw new JsonSerializationException($"Error converting {reader.Value ?? "Null"} to {objectType.Name}.", ex);
        }
    }

    public override void WriteJson(JsonWriter writer, TEnum value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteValue(value.Value);
        }
    }
}
