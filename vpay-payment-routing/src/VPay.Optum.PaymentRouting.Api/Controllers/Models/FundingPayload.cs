using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.MassTransit.Serialization;

namespace VPay.Optum.PaymentRouting.Api.Controllers.Models
{
    public class FundingPayload
    {
        public string BatchID { get; set; }
        public string PayerID { get; set; }
        public int? TotalPaymentCount { get; set; }
        public decimal? TotalPaymentAmount { get; set; }

        public VpayFunding ToAvro()
        {
            return new VpayFunding
            {
                batchID = BatchID,
                payer835ID = PayerID,
                totalPaymentCount = TotalPaymentCount,
                totalPaymentAmount = TotalPaymentAmount.ToBytes()
            };
        }

        public static FundingPayload Create(VpayFunding avro)
        {
            return new FundingPayload
            {
                BatchID = avro.batchID,
                PayerID = avro.payer835ID,
                TotalPaymentCount = avro.totalPaymentCount,
                TotalPaymentAmount = avro.totalPaymentAmount.FromBytes()
            };
        }
    }
}
