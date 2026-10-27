using Newtonsoft.Json;
using VPay.Gateway.Common.Helpers;

namespace Vpay.Gateway.Common.Vpay.Models;

public class TransactionEvent
{
    public string Status { get; set; } = string.Empty;

    public string RequesterName { get; set; } = string.Empty;

    public int RequesterId { get; set; }
    //
    // Summary:
    //     The authcode.
    public string AuthCode { get; set; } = string.Empty;

    //
    // Summary:
    //     The action code.
    [JsonIgnore]
    public string ActionCode { get; set; } = string.Empty;

    //
    // Summary:
    //     The accounting type.
    public string AccountingType { get; set; } = string.Empty;

    [JsonConverter(typeof(DecimalJsonConverter))]
    public decimal? Amount { get; set; }

    //
    // Summary:
    //     The shipping class.
    public string ShippingClass { get; set; } = string.Empty;


    //
    // Summary:
    //     The tracking number.
    public string TrackingNumber { get; set; } = string.Empty;

    //
    // Summary:
    //     Time of the transaction.
    public DateTime Timestamp { get; set; }

    //
    // Summary:
    //     The code associated with the reason for the event.
    public string ReasonCode { get; set; } = string.Empty;


    //
    // Summary:
    //     The description of the reason for the event.
    public string ReasonDescription { get; set; } = string.Empty;

    //
    // Summary:
    //     The information for the recipient to which a payment is sent; also referred to
    //     as the Payee.
    public string MerchantInfo { get; set; } = string.Empty;

    //
    // Summary:
    //     The merchant code.
    public string MerchantCode { get; set; } = string.Empty;

    //
    // Summary:
    //     The transaction.
    [JsonIgnore]
    public string Transaction { get; set; } = string.Empty;

    //
    // Summary:
    //     The expiration date.
    [JsonIgnore]
    public DateTime? Expiration { get; set; }

    //
    // Summary:
    //     The batch identifier.
    public long BatchId { get; set; }

}
