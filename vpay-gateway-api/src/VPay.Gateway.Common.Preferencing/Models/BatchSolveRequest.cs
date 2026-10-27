using System.ComponentModel.DataAnnotations;

namespace VPay.Gateway.Common.Preferencing.Models;

public class BatchSolveRequest
{
    /// <summary>
    /// The Correlation Id for a batch of <see cref="BatchSolveRequest"/>
    /// </summary>
    public string CorrelationId { get; set; } = null!;

    /// <summary>
    /// The Origin Code
    /// </summary>
    public string OriginCode { get; set; } = null!;

    /// <summary>
    /// The VPAY Client Code (TPA)
    /// </summary>
    public string ClientCode { get; set; } = null!;

    /// <summary>
    /// The VPAY Billing Entity Code
    /// </summary>
    public string BillingEntityCode { get; set; } = null!;

    /// <summary>
    /// The Tax Id (TIN)
    /// </summary>
    public string? TaxId { get; set; }

    /// <summary>
    /// The Provider's Name
    /// </summary>
    public string? ProviderName { get; set; }

    /// <summary>
    /// The Provider's Address
    /// </summary>
    public string? ProviderAddress1 { get; set; }

    /// <summary>
    /// The Provider Type (MEDICAL, MEMBER, etc.)
    /// </summary>
    public string ProviderType { get; set; } = null!;

    /// <summary>
    /// The Zip Code (5 digit, 9 digit, 9 digit w/ dash)
    /// </summary>
    public string? ZipCode { get; set; }

    /// <summary>
    /// The Amount ($)
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// The Date
    /// </summary>
    [DataType(DataType.Date)]
    public DateOnly? Date { get; set; }
}
