using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VPay.Optum.Http.Transactions
{
    public class ISeriesDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(typeToConvert == typeof(DateTime));

            if (!reader.TryGetDateTime(out DateTime value))
            {
                CultureInfo provider = CultureInfo.InvariantCulture;

                value = DateTime.ParseExact(reader.GetString(), "yyyy-MM-dd-HH.mm.ss.FFFFFFF", provider);
            }

            return value;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-dd-HH.mm.ss.FFFFFFF"));
        }
    }
}