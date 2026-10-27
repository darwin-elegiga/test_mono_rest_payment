using System.Text.Json.Serialization;

namespace VPay.Distribution.Common
{
    public abstract class StpRemittanceDocumentBase
    {
        public class RemittanceHeader
        {
            public string NetworkPartner { get; set; }
            public string OptumTransactionId { get; set; }
        }

        public class RemittancePayee
        {
            public string Name { get; set; }
            public string Tin { get; set; }
            public string AddressLine1 { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string Zip { get; set; }
            public string Uuid { get; set; }
        }

        public class RemittancePayer
        {
            public string TraceNumber { get; set; }
            public string Tpa { get; set; }
            public string Address { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string Zip { get; set; }
        }

        [JsonPropertyOrder(-1)]
        public RemittanceHeader Header { get; set; }

        [JsonPropertyOrder(1)]
        public RemittancePayee Payee { get; set; }

        [JsonPropertyOrder(2)]
        public RemittancePayer Payer { get; set; }

        public abstract string ToJsonString();
    }
}
