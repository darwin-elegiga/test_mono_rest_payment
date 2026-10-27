using System;
using VPay.Optum.PaymentRouting.Data.SmartEnums;

namespace VPay.Optum.PaymentRouting.Data.Sql.Models
{
    public class ApMessageTracking
    {
        public long ApMessageTrackingId { get; set; }
        public long ApMessageId { get; set; }
        public long InboundPaymentId { get; set; }
        public long TransactionId { get; set; }
        public string TrnSegment { get; set; }
        public decimal LoadAmount { get; set; }
        public string PaymentTypeCode { get; set; }
        public string CheckNumber { get; set; }
        public DateTime LoadDate { get; set; }
        public ApMessageTrackingStatus Status { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string LastModifiedBy { get; set; }
        public DateTime LastModifiedDateTime { get; set; }
    }
}
