namespace VPay.Gateway.Api.Models;

public class DocumentDto
{
    public long? TransactionId { get; set; }

    public long? DocumentId { get; set; }

    public string FileName { get; set; }

    public string FileDetailType { get; set; }

    public string Description { get; set; }
}
