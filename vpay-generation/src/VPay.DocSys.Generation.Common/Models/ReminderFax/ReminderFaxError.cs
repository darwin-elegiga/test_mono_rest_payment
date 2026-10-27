using System;

using VPay.Data.Db2.Abstractions.Fax;

using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation.Common
{
    public class ReminderFaxError
    {
        public ReminderFaxError(ReminderFax reminderFax, Exception exception = null)
        {
            ThrowIf.IsNull(reminderFax, nameof(reminderFax));

            ClientType = reminderFax.ClientType;
            OriginCode = reminderFax.OriginCode;
            ClientCode = reminderFax.TPACode;
            BillingEntity = reminderFax.BillingEntity;
            PaymentTypeCode = reminderFax.PaymentTypeCode;
            TransactionId = reminderFax.SeTransactionId;
            ExceptionMessage = exception?.Message;
        }

        public ClientType ClientType { get; }
        public string ReminderType { get; }
        public string OriginCode { get; }
        public string ClientCode { get; }
        public string BillingEntity { get; }
        public string PaymentTypeCode { get; }
        public long TransactionId { get; }
        public string ExceptionMessage { get; }
    }
}
