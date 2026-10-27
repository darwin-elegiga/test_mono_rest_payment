namespace VPay.Optum.PaymentRouting.Common.Configuration
{
    public class OptumQueueNameSettings
    {
        public string Payment { get; set; } = "sbnamespace-vpay-prs-dev1-queue";
        public string Funding { get; set; } = "sbnamespace-vpay-funding-prs-dev1-queue";
        public string AccountPayable { get; set; } = "sbnamespace-ap-prs-dev1-queue";
    }
}
