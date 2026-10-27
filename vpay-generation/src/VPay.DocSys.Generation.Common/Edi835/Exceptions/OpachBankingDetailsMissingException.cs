using System;

namespace VPay.DocSys.Generation.Common.Edi835
{
    [Serializable]
    public class OpachBankingDetailsMissingException : Exception
    {
        public OpachBankingDetailsMissingException(long transactionId)
        {
            TransactionId = transactionId;
        }

        public OpachBankingDetailsMissingException(string message, long transactionId) : base(message)
        {
            TransactionId = transactionId;
        }

        public OpachBankingDetailsMissingException(string message, long transactionId, Exception inner) : base(message, inner)
        {
            TransactionId = transactionId;
        }

        public long TransactionId { get; }
    }
}
