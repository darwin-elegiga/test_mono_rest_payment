using System.Collections.Generic;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Used for swagger endpoint request to pass a list of transactionIds
    /// </summary>
    public class TransactionIdListRequest
    {
        public List<long> TransactionIds { get; set; }
    }
}
