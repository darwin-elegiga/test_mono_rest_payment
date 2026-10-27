using System;
using Newtonsoft.Json;

namespace VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto
{
    public class EdiDeliveryDestinationEnumConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(bool);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var value = reader.Value?.ToString().ToLower().Trim() ?? string.Empty;

            switch (value)
            {
                case "availity":
                    return EdiDeliveryDestination.Availity;
                default:
                    break;
            }

            return EdiDeliveryDestination.None;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
                var extension = EdiDeliveryDestination.None;

                if (value is EdiDeliveryDestination e)
                {
                    extension = e;
                }

                switch (extension)
                {
                    case EdiDeliveryDestination.Availity:
                        writer.WriteValue("Availity");
                        break;
                    default:
                        writer.WriteValue("");
                        break;
                }
        }
    }
}
