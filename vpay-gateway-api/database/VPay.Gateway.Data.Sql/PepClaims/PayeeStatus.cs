namespace VPay.Gateway.Data.Sql.PepClaims;

public class PayeeStatus
{
    public long Id { get; set; }

    public long PayeeId { get; set; }

    public long PepPayeeId { get; set; }

    public string? ClaimId { get; set; }

    public string? Status { get; set; }

    public string? CurrentProcessStatus { get; set; }

    public long PepStatusId { get; set; }

    public DateTime StatusDateTime { get; set; }

    public DateTime CreatedDateTime { get; set; }

    public DateTime LastModifiedDateTime { get; set; }

    public long? PartyStatusId { get; set; }

    public string? PartyStatus { get; set; }

    public int? RetryCount { get; set; }
}
