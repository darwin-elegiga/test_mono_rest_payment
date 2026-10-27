namespace VPay.Gateway.Common.PepClaims.Models;

public class PartyDto
{
    public long Id { get; set; }

    public long HeaderId { get; set; }

    public string ClaimId { get; set; } = "";

    public string PaymentId { get; set; } = "";

    public string MemberId { get; set; } = "";

    public string ClientAuthentication { get; set; } = "";

    public string AuthenticationLabel1 { get; set; } = "";

    public string AuthenticationValue1 { get; set; } = "";

    public string AuthenticationLabel2 { get; set; } = "";

    public string AuthenticationValue2 { get; set; } = "";

    public string PartyType { get; set; } = "";

    public string PaymentRequestType { get; set; } = "";

    public string Endorser { get; set; } = "";

    public string Payee { get; set; } = "";

    public string PartyName { get; set; } = "";

    public string DefaultAddress { get; set; } = "";

    public string PartyAddress1 { get; set; } = "";

    public string PartyAddress2 { get; set; } = "";

    public string PartyAddress3 { get; set; } = "";

    public string PartyCity { get; set; } = "";

    public string PartyState { get; set; } = "";

    public string PartyCountry { get; set; } = "";

    public string PartyPostalCode { get; set; } = "";

    public string PartyEmail { get; set; } = "";

    public string Phone1 { get; set; } = "";

    public string Phone1Type { get; set; } = "";

    public string Phone2 { get; set; } = "";

    public string Phone2Type { get; set; } = "";

    public string PartyFaxNumber { get; set; } = "";

    public string GovernmentIdNumber { get; set; } = "";

    public string GovernmentIdType { get; set; } = "";

    public string BankRoutingNumber { get; set; } = "";

    public string BankAccountNumber { get; set; } = "";

    public string PreferredDocumentationDistributionMethod { get; set; } = "";

    public string PreferredContactMethod { get; set; } = "";

    public DateTime Created { get; set; }

    public bool IsDesignatedPayee { get; set; }

    public ICollection<PartyStatusDto> PartyStatuses { get; set; } = new List<PartyStatusDto>();
}
