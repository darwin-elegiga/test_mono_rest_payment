using System.Collections.Generic;

namespace VPay.Distribution.Common
{
    public class EventPublishResponse
    {
        public EventPublishResponse()
        {
            TransactionsWithEventsPublished = new HashSet<long>();

            TransactionsNotDistributed = new HashSet<long>();

            TransactionsWithoutDistributionStatus = new HashSet<long>();

            Notes = string.Empty;
        }

        public HashSet<long> TransactionsWithEventsPublished { get; set; }

        public HashSet<long> TransactionsNotDistributed { get; set; }

        public HashSet<long> TransactionsWithoutDistributionStatus { get; set; }

        public string Notes { get; set; }
    }
}
