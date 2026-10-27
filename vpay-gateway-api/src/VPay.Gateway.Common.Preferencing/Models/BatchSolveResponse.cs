namespace VPay.Gateway.Common.Preferencing.Models;
public class BatchSolveResponse
{
    /// <summary>
    /// The Correlation Id for a batch of <see cref="BatchSolveResponse"/>
    /// </summary>
    public string CorrelationId { get; set; } = null!;

    /// <summary>
    /// The Payment Type Code (CLCFT, CLCHK, etc.)
    /// </summary>
    public string PaymentTypeCode { get; set; } = null!;

    /// <summary>
    /// The Distribution Method ('M', 'F', 'Z', etc.)
    /// </summary>
    public string? DistributionMethod { get; set; }

    /// <summary>
    /// The Fax Number (if applicable)
    /// </summary>
    public string? FaxNumber { get; set; }

    /// <summary>
    /// The Voice Number (if applicable)
    /// </summary>
    public string? VoiceNumber { get; set; }

    /// <summary>
    /// The Original Payment Type Code (if the payment type has changed)
    /// </summary>
    public string? OriginalPaymentTypeCode { get; set; }

    /// <summary>
    /// The distribution method description corresponding to the distribution method:
    /// M - Mail, F - Fax, Z - Mail Due to No Fax, N - No Distribution
    /// </summary>
    public string? DistributionDescription { get; set; }
}
