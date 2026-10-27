using System.Text.Json;

namespace VPay.Distribution.Common
{
    public class StpC2BRemittanceDocument : StpRemittanceDocumentBase
    {
        public class RemittanceConsumer
        {
            public string PatientName { get; set; }
            public string PatientDOB { get; set; }
            public string ClaimId { get; set; }
            public string PatientAccountNumber { get; set; }
            public string Amount { get; set; }
            public string ClaimDate { get; set; }
            public string SubscriberName { get; set; }
            public string ClientReferenceId { get; set; }
        }

        public RemittanceConsumer Consumer { get; set; }

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
