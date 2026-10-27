using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto;

/// <summary>
/// Represents an Prs Mapping Record
/// </summary>
[DebuggerDisplay("{ClientCode,nq} - {BillingEntityCode,nq} - {OptumPayorId,nq} - {OptumSubPayorId,nq}")]
public sealed class PrsMappingResult
{
    /// <summary>
    /// Id.
    /// </summary>
    [Column("Id")]
    public int Id { get; set; } = 0;

    /// <summary>
    /// The client code.
    /// </summary>
    [Column("ClientCode")]
    public string ClientCode { get; set; } = "";

    /// <summary>
    /// The billing entity code.
    /// </summary>
    [Column("BillingEntityCode")]
    public string BillingEntityCode { get; set; } = "";

    /// <summary>
    /// The Optum Payor Id.
    /// </summary>
    [Column("OptumPayorId")]
    public string OptumPayorId { get; set; } = "";

    /// <summary>
    /// The Optum Sub Payor Id
    /// </summary>
    [Column("OptumSubPayorId")]
    public string OptumSubPayorId { get; set; } = "";

    /// <summary>
    /// The Date the Mapping was Created.
    /// </summary>
    [Column("CreatedDateTime")]
    public DateTime CreatedDateTime { get; set; }

    /// <summary>
    /// The Id of the person that created the mapping
    /// </summary>
    [Column("CreatedBy")]
    public string CreatedBy { get; set; } = "";

    /// <summary>
    /// The date the mapping was modified.
    /// </summary>
    [Column("LastModifiedDateTime")]
    public DateTime? LastModifiedDateTime { get; set; }

    /// <summary>
    /// The Id of the user that updated the mapping
    /// </summary>
    [Column("LastModifiedBy")]
    public string LastModifiedBy { get; set; }
}
