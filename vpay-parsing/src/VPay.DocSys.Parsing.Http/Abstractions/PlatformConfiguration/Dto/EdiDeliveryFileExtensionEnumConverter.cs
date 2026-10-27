using System;
using Newtonsoft.Json;

namespace VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto
{
    public class EdiDeliveryFileExtensionEnumConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(EdiDeliveryFileExtension);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var value = reader.Value?.ToString().ToLower().Trim() ?? string.Empty;

            switch (value)
            {
                case ".835":
                case "835":
                    return EdiDeliveryFileExtension.Edi835;
                case ".brn":
                case "brn":
                    return EdiDeliveryFileExtension.EdiBRN;
                default:
                    break;
            }

            return EdiDeliveryFileExtension.None;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var extension = EdiDeliveryFileExtension.None;

            if (value is EdiDeliveryFileExtension e)
            {
                extension = e;
            }

            switch (extension)
            {
                case EdiDeliveryFileExtension.Edi835:
                    writer.WriteValue(".835");
                    break;
                case EdiDeliveryFileExtension.EdiBRN:
                    writer.WriteValue(".brn");
                    break;
                default:
                    writer.WriteValue("");
                    break;
            }
        }
    }
}
