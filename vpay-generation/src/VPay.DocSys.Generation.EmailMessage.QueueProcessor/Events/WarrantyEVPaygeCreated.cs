namespace VPay.DocSys.Generation.EmailMessage.QueueProcessor.Events;

public record WarrantyEVPaygeCreated
{
    public Guid Id { get; init; }
    public string Tpa { get; init; }
    public string VPTransId { get; init; }
    public string EmailAddress { get; init; }
    public string VPaygeXmlBase64 { get; init; }
    public string VPayge { get; init; }
}
