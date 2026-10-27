using System.Collections.Generic;

namespace VPay.Optum.PaymentRouting.Api.Controllers.Models
{
    public class ApReapDbPayload
    {
        public IEnumerable<long> TransactionIds { get; set; } = [];

        public string ApMessageType { get; set; } = null;

        public ApReapDbPayload()
        {
        }

        public ApReapDbPayload(IEnumerable<long> transactionIds, string apMessageType = null)
        {
            TransactionIds = transactionIds;
            ApMessageType = apMessageType;
        }
    }
}
