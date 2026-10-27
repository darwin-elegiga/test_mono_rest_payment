using Newtonsoft.Json;

namespace VPay.Gateway.Common.Enrollments.Models;

public class PartyEnrollmentRequest
{
    [JsonProperty(PropertyName = "Origin")]
    public string? Origin { get; set; }

    [JsonProperty(PropertyName = "TPA")]
    public string? TPA { get; set; }

    [JsonProperty(PropertyName = "BillingEntity")]
    public string? BillingEntity { get; set; }

    [JsonProperty(PropertyName = "BillingEntityName")]
    public string? BillingEntityName { get; set; }

    [JsonProperty(PropertyName = "TPA_Full_Name")]
    public string? TPAFullName { get; set; }

    [JsonProperty(PropertyName = "Date of File Timestamp")]
    public string? DateOfFileTimestamp { get; set; }

    [JsonProperty(PropertyName = "timeout_date")]
    public string? TimeoutDate { get; set; }

    [JsonProperty(PropertyName = "FullColorLogo")]
    public string? FullColorLogo { get; set; }

    [JsonProperty(PropertyName = "Timeout Parameter")]
    public string? TimeoutParameter { get; set; }

    [JsonProperty(PropertyName = "Additional Field Label 1")]
    public string? AdditionalFieldLabel1 { get; set; }

    [JsonProperty(PropertyName = "Additional Field Value 1")]
    public string? AdditionalFieldValue1 { get; set; }

    [JsonProperty(PropertyName = "Additional Field Label 2")]
    public string? AdditionalFieldLabel2 { get; set; }

    [JsonProperty(PropertyName = "Additional Field Value 2")]
    public string? AdditionalFieldValue2 { get; set; }

    [JsonProperty(PropertyName = "Additional Field Label 3")]
    public string? AdditionalFieldLabel3 { get; set; }

    [JsonProperty(PropertyName = "Additional Field Value 3")]
    public string? AdditionalFieldValue3 { get; set; }

    [JsonProperty(PropertyName = "Authentication Label 1")]
    public string? AuthenticationLabel1 { get; set; }

    [JsonProperty(PropertyName = "Authentication Label 2")]
    public string? AuthenticationLabel2 { get; set; }

    [JsonProperty(PropertyName = "Authentication Value 1")]
    public string? AuthenticationValue1 { get; set; }

    [JsonProperty(PropertyName = "Authentication Value 2")]
    public string? AuthenticationValue2 { get; set; }

    [JsonProperty(PropertyName = "Client Authentication")]
    public string? ClientAuthentication { get; set; }

    [JsonProperty(PropertyName = "Member ID")]
    public string? MemberID { get; set; }

    [JsonProperty(PropertyName = "Payee Address 1")]
    public string? PayeeAddress1 { get; set; }

    [JsonProperty(PropertyName = "Payee Address 2")]
    public string? PayeeAddress2 { get; set; }

    [JsonProperty(PropertyName = "Payee Address 3")]
    public string? PayeeAddress3 { get; set; }

    [JsonProperty(PropertyName = "Payee City")]
    public string? PayeeCity { get; set; }

    [JsonProperty(PropertyName = "Payee Country")]
    public string? PayeeCountry { get; set; }

    [JsonProperty(PropertyName = "Payee Email")]
    public string? PayeeEmail { get; set; }

    [JsonProperty(PropertyName = "Payee Name")]
    public string? PayeeName { get; set; }

    [JsonProperty(PropertyName = "Payee Postal Code")]
    public string? PayeePostalCode { get; set; }

    [JsonProperty(PropertyName = "Payee State")]
    public string? PayeeState { get; set; }

    [JsonProperty(PropertyName = "Phone 1")]
    public string? Phone1 { get; set; }

    [JsonProperty(PropertyName = "Phone 1 Type")]
    public string? Phone1Type { get; set; }

    [JsonProperty(PropertyName = "Phone 2")]
    public string? Phone2 { get; set; }

    [JsonProperty(PropertyName = "Phone 2 Type")]
    public string? Phone2Type { get; set; }

    [JsonProperty(PropertyName = "Policy Number")]
    public string? PolicyNumber { get; set; }
}
