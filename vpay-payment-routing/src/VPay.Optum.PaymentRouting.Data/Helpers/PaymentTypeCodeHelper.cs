using VPay.EdiFabric.Helpers.Models;

namespace VPay.Optum.PaymentRouting.Data.Helpers
{
    public static class PaymentTypeCodeHelper
    {
        //TODO: check if correct values and move to package?
        public static PaymentType StringToPaymentType(string paymentTypeCode)
        {
            return paymentTypeCode switch
            {
                "CLSDA" => PaymentType.ACH,
                "CLEFT" => PaymentType.ACH,
                "CLVCP" => PaymentType.Card,
                "CLCHK" => PaymentType.Check,
                "OPACH" or "CLCFT" => PaymentType.CheckToACH,
                _ => PaymentType.NonPayment
            };
        }
    }
}
