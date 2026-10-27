using System.Text.Json;

namespace VPay.DocSys.Generation
{
    public class StpB2BRemittanceDocument : StpRemittanceDocumentBase
    {
        public override string ToJsonString()
        {
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            return JsonSerializer.Serialize(this, serializeOptions);
        }
    }
}
