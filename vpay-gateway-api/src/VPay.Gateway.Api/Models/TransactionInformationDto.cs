namespace VPay.Gateway.Api.Models;

public sealed record TransactionInformationDto
{
    /// <summary>
    /// The client the transaction is associated with.
    /// </summary>
    /// <example>TPA</example>
    public string ClientCode { get; init; }

    /// <summary>
    /// The billing entity the transaction is associated with.
    /// </summary>
    /// <example>TPA0001001</example>
    public string BillingEntityCode { get; init; }

    /// <summary>
    /// The internal transaction id.
    /// </summary>
    /// <example>726854676</example>
    public long TransactionId { get; init; }

    /// <summary>
    /// The client's payment id.
    /// </summary>
    /// <example>TPATX001776354</example>
    public string ClientPaymentId { get; init; }
}
