namespace VPay.Gateway.Data.Sql.PepClaims;

public class EnrollmentStatus : PayeeStatus
{
    public string? ClientCode { get; set; }
    public string? MemberId { get; set; }
}
