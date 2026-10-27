namespace VPay.DocSys.Parsing.Common
{
    public class StpPaymentCreatedGroupDto
    {
        public string NetworkName { get; set; } = string.Empty;
        public string EnrollType { get; set; } = string.Empty;
        public string Edi835Type { get; set; } = string.Empty;
        public int PaymentCount { get; set; }
        public bool IsSuccessful { get; set; }
    }
}
