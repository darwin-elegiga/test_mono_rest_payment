namespace VPay.Gateway.Data.Sql.Transactions;

public class TransactionNetwork
{
    public long Id { get; set; }
    public long TransactionId { get; set; }
    public string NetworkName { get; set; } = string.Empty;
    public string EnrollmentType { get; set; } = string.Empty;
    public string NetworkType { get; set; } = string.Empty;
}
