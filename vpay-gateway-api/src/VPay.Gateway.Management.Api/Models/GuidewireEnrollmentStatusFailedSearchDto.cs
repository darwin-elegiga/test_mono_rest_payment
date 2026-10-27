using System.ComponentModel;
using VPay.Gateway.Data.Paging;

namespace VPay.Gateway.Management.Api.Models;

public class GuidewireEnrollmentStatusFailedSearchDto
{
    /// <summary>
    /// The optional client code filter, or null for enrollments from any client.
    /// The default is null.
    /// </summary>
    [DefaultValue(null)]
    public string ClientCode { get; set; } = null;

    /// <summary>
    /// The optional member id filter, or null for enrollments from any member.
    /// The default is null.
    /// </summary>
    [DefaultValue(null)]
    public string MemberId { get; set; } = null;

    /// <summary>
    /// The optional claim number filter, or null for enrollments from any claim.
    /// The default is null.
    /// </summary>
    [DefaultValue(null)]
    public string ClaimNumber { get; set; } = null;

    /// <summary>
    /// Index of the page to retrieve. Page index starts at 1.
    /// Optional, defaults to 1 when null or less than 1.
    /// </summary>
    [DefaultValue(Defaults.DefaultPage)]
    public int? Page { get; set; } = Defaults.DefaultPage;

    /// <summary>
    /// The number of results per page. Can be any value greater than 0.
    /// Optional, defaults to 50 when null or less than 1.
    /// </summary>
    [DefaultValue(Defaults.DefaultPageSize)]
    public int? PageSize { get; set; } = Defaults.DefaultPage;
}
