using System.Security.Claims;
using VPay.Extensions.UserManagement.Abstractions;
using VPay.Gateway.Application;
using VPay.Gateway.Identity;

namespace VPay.Gateway.Api;

public class UserInfo : IUserInfo
{
    public const string PaymentTypeRestrictedRoleName = "OPACHOnly";
    private readonly IHttpContextAccessor _accessor;

    public UserInfo(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string ClientId => _accessor.HttpContext.User.FindFirstValue(ClaimNames.ClientId) ?? "";

    public string DisplayName => _accessor.HttpContext.User.FindFirstValue(ClaimNames.DisplayName) ?? "";

    public long? UserId
    {
        get
        {
            var id = _accessor.HttpContext.User.FindFirstValue(ClaimNames.EntityId);
            if (long.TryParse(id, out var result))
            {
                return result;
            }
            return null;
        }
    }

    public IEnumerable<string> ClientCodes => _accessor.HttpContext.User.FindAll(ClaimNames.ClientCode).Select(x => x.Value);
    public IEnumerable<string> BillingEntityCodes => _accessor.HttpContext.User.FindAll(ClaimNames.BillingEntityCode).Select(x => x.Value);
    public IEnumerable<string> BillingEntityExclusions => _accessor.HttpContext.User.FindAll(ClaimNames.BillingEntityExclusion).Select(x => x.Value);

    public bool IsAuthenticated => _accessor.HttpContext.User.Identity.IsAuthenticated;

    public bool CanAccess(string clientCode, string billingEntityCode)
    {
        if (!IsAuthenticated)
        {
            return false;
        }

        var accessFilter = new AccessFilter(ClientCodes.ToList(), BillingEntityCodes.ToList(), BillingEntityExclusions.ToList());

        return accessFilter.CanAccess(clientCode, billingEntityCode);
    }

    public bool CanAccess(string clientCode, string billingEntityCode, string paymentTypeCode)
    {
        if (!CanAccess(clientCode, billingEntityCode))
        {
            return false;
        }

        if (IsInRoleWithRestrictedPaymentTypes())
        {
            return (
                   string.Equals(paymentTypeCode, "OPACH", StringComparison.Ordinal)
                || string.Equals(paymentTypeCode, "CLCFT", StringComparison.Ordinal)
                || string.Equals(paymentTypeCode, "CLNPF", StringComparison.Ordinal)
                );
        }

        return true;
    }

    public bool IsInRole(string roleName)
    {
        return _accessor.HttpContext.User.IsInRole(roleName);
    }

    public bool IsInRoleWithRestrictedPaymentTypes()
    {
        return IsInRole(PaymentTypeRestrictedRoleName);
    }
}
