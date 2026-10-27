namespace VPay.Gateway.Application;

public interface IUserInfo
{
    string ClientId { get; }

    string DisplayName { get; }

    long? UserId { get; }

    IEnumerable<string> ClientCodes { get; }

    IEnumerable<string> BillingEntityCodes { get; }

    IEnumerable<string> BillingEntityExclusions { get; }

    bool CanAccess(string clientCode, string billingEntityCode);

    bool CanAccess(string clientCode, string billingEntityCode, string paymentTypeCode);

    bool IsAuthenticated { get; }

    bool IsInRole(string roleName);

    bool IsInRoleWithRestrictedPaymentTypes();
}
