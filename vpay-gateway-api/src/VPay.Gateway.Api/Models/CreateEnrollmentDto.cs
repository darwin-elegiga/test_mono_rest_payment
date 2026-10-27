using System.ComponentModel.DataAnnotations;

namespace VPay.Gateway.Api.Models;

public class CreateEnrollmentDto
{
    /// <summary>
    /// The client code assigned to the caller
    /// </summary>
    public string ClientCode { get; set; }

    /// <summary>
    /// The funding account code assigned to the caller
    /// </summary>
    public string FundingAccountCode { get; set; }

    /// <summary>
    /// The client-generated member id.
    /// </summary>
    public string MemberId { get; set; }

    /// <summary>
    /// The policy number.
    /// </summary>
    public string PolicyNumber { get; set; }

    /// <summary>
    /// The name of the payee.
    /// </summary>
    public string PayeeName { get; set; }

    /// <summary>
    /// The email for the payee.
    /// </summary>
    [DataType(DataType.EmailAddress)]
    public string PayeeEmail { get; set; }

    /// <summary>
    /// The cell number for the payee.
    /// </summary>
    /// <example>5558675309</example>
    [DataType(DataType.PhoneNumber)]
    public string CellPhoneNumber { get; set; }
}
