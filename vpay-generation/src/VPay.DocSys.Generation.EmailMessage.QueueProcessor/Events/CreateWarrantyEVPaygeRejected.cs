namespace VPay.DocSys.Generation.EmailMessage.QueueProcessor.Events;

public record CreateWarrantyEVPaygeRejected
{
    public Guid Id { get; init; }
    public string Tpa { get; init; }
    public string VPTransId { get; init; }
    public string EmailAddress { get; init; }
}
