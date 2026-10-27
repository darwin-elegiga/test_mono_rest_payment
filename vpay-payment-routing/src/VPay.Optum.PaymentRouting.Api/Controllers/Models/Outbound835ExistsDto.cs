namespace VPay.Optum.PaymentRouting.Api.Controllers.Models
{
    public class Outbound835ExistsDto
    {
        public long TransactionId { get; set; }
        public bool Exists { get; set; }
    }
}
