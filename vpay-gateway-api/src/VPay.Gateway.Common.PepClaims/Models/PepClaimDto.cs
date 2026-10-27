namespace VPay.Gateway.Common.PepClaims.Models;

public class PepClaimDto
{
    public string Id { get; set; } = "";

    public string BillingEntityId { get; set; } = "";

    public string BillingEntityName { get; set; } = "";

    public string ClaimNumber { get; set; } = "";

    public string Tpa { get; set; } = "";

    public string ClientName { get; set; } = "";

    public string ClientAddress { get; set; } = "";

    public string ClientPhoneNumber { get; set; } = "";

    public string ClientReferenceId { get; set; } = "";

    public DateTime ExpirationTime { get; set; }

    public string FileId { get; set; } = "";

    public long HeaderId { get; set; }

    public Guid MasterGuid { get; set; }

    public string Origin { get; set; } = "";

    public string PaymentId { get; set; } = "";

    public string PayerId { get; set; } = "";

    public DateTime PayerTimestamp { get; set; }

    public decimal PaymentAmount { get; set; }

    public string PaymentCurrency { get; set; } = "";

    public string RemitMethod { get; set; } = "";

    public long PartyCount { get; set; }

    public int? TimeoutParameterMinutes { get; set; }

    public string TimeoutOutcome { get; set; } = "";

    public string PartiesDisagreeOutcome { get; set; } = "";

    public string JurisdictionalBody { get; set; } = "";

    public string PepEligible { get; set; } = "";

    public string ShippingName { get; set; } = "";

    public string ShippingAddress1 { get; set; } = "";

    public string ShippingAddress2 { get; set; } = "";

    public string ShippingAddress3 { get; set; } = "";

    public string ShippingCity { get; set; } = "";

    public string ShippingState { get; set; } = "";

    public string ShippingCountry { get; set; } = "";

    public string ShippingPostalCode { get; set; } = "";

    public string PolicyNumber { get; set; } = "";

    public string PreservePepUsers { get; set; } = "";

    public DateTime Created { get; set; }

    public string EnrollmentType { get; set; } = "";
}
