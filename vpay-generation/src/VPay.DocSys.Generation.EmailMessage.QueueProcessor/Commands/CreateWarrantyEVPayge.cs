namespace VPay.DocSys.Generation.EmailMessage.QueueProcessor.Commands
{
    public record CreateWarrantyEVPayge
    {
        public Guid Id { get; init; }
        public string Tpa { get; init; }
        public string VPTransId { get; init; }
        public string EmailAddress { get; init; }
        public string VPaygeXmlBase64 { get; init; }
    }
}
