namespace VPay.Gateway.Api.Models;

public class PaymentRejectionRequestDto
{
    /// <summary>
    /// The network partner name.
    /// </summary>
    /// <example>OPTUM</example>
    public string NetworkPartner { get; init; }

    /// <summary>
    /// The transaction id.
    /// </summary>
    /// <example>123456789</example>
    public long? OptumTransactionId { get; init; }

    /// <summary>
    /// The rejection reason code.
    /// </summary>
    /// <example>UTLA</example>
    public string RejectionReasonCode { get; init; }

    /// <summary>
    /// The rejection reason description.
    /// </summary>
    /// <example>Unable To Locate Payment.</example>
    public string RejectionReason { get; init; }
}
