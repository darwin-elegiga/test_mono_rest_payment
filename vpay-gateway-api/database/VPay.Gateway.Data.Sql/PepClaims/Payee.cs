namespace VPay.Gateway.Data.Sql.PepClaims;

public class Payee
{
    public long Id { get; set; }

    public string? OriginCode { get; set; }

    public string? ClientCode { get; set; }

    public string? MemberId { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public DateTime CreatedDateTime { get; set; }

    public DateTime LastModifiedDateTime { get; set; }
}
