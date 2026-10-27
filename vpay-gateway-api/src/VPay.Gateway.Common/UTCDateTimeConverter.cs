using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VPay.Gateway.Common;

/// <summary>
/// Converts a <see cref="DateTime"/> to and from the ISO 8601 date format (e.g. "2008-04-12T12:53Z").
/// When a value is being read with a <see cref="DateTimeKind"/> of <see cref="DateTimeKind.Unspecified"/>
/// the date will be considered to be in the America/Chicago timezone, and will be converted to UTC.
/// When a value is being read with a <see cref="DateTimeKind"/> of <see cref="DateTimeKind.Local"/>
/// the date will be converted to UTC.
/// </summary>
public class UTCDateTimeConverter : IsoDateTimeConverter
{
    private readonly TimeZoneInfo _centralTimeZone;

    public UTCDateTimeConverter()
    {
        _centralTimeZone =
            TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(x => x.Id == "Central Standard Time") ??
            TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"); // IANA format for Linux compatibility.
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var result = base.ReadJson(reader, objectType, existingValue, serializer);
        if (result is DateTime dt)
        {
            // When reading the value, convert it to UTC
            return dt.Kind switch
            {
                DateTimeKind.Unspecified => TimeZoneInfo.ConvertTimeToUtc(dt, _centralTimeZone),
                DateTimeKind.Local => dt.ToUniversalTime(),
                _ => dt
            };
        }

        return result;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is DateTime dt)
        {
            // when writing the value, make sure it's UTC
            base.WriteJson(writer, dt.ToUniversalTime(), serializer);
        }
        else
        {
            base.WriteJson(writer, value, serializer);
        }
    }
}
