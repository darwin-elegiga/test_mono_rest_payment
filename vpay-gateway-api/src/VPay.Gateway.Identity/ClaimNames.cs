using System.Security.Claims;

namespace VPay.Gateway.Identity;

public static class ClaimNames
{
    /// <summary>
    /// The ID of the SQL entity which stores this user's info.
    /// </summary>
    public static readonly string EntityId = "vpay:entityId";

    public static readonly string ClientId = ClaimTypes.NameIdentifier;

    public static readonly string DisplayName = ClaimTypes.Name;

    public static readonly string ClientCode = "vpay:clientCode";

    public static readonly string BillingEntityCode = "vpay:billingEntityCode";

    public static readonly string BillingEntityExclusion = "vpay:billingEntityExclusion";
}
