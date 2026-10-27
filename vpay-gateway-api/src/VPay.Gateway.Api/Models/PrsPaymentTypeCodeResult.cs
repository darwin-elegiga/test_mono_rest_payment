namespace VPay.Gateway.Api.Models;

public struct PrsPaymentTypeCodeResult
{
    /// <summary>
    /// Unique Identifier for request
    /// </summary>
    public string CorrelationID { get; set; }
    /// <summary>
    /// Payment type code
    /// </summary>
    public string PaymentTypeCode { get; set; }

    /// <summary>
    /// The distribution method description corresponding to the distribution method:
    /// M - Mail, F - Fax, Z - Mail Due to No Fax, N - No Distribution
    /// </summary>
    public string DistributionDescription { get; set; }
}
