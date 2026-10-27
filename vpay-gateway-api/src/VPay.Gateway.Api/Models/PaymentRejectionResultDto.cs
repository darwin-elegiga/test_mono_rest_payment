namespace VPay.Gateway.Api.Models;

public class PaymentRejectionResultDto
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
    public long OptumTransactionId { get; init; }

    /// <summary>
    /// The response message.
    /// </summary>
    /// <example>Do STP DTP Completion</example>
    public string ResponseMessage { get; init; }
}
