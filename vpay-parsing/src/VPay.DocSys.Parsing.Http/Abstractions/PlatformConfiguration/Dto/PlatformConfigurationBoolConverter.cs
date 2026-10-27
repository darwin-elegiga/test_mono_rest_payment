using System;
using Newtonsoft.Json;

namespace VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto
{

    public class PlatformConfigurationBoolConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(bool);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                var value = reader.Value.ToString().ToLower().Trim();
                switch (value)
                {
                    case "true":
                    case "t":
                    case "yes":
                    case "y":
                    case "1":
                        return true;
                }
                return false;
            }
            catch (FormatException ex)
            {
                //ASP.net will catch this error and send a bad request, it does not currently catch a Format Exception error
                throw new JsonSerializationException(ex.Message, ex);
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is bool && (bool)value)
            {
                writer.WriteValue("Y");
            }
            else
            {
                writer.WriteValue("N");
            }
        }
    }
}
