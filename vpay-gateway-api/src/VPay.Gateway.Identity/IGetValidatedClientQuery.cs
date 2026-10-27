using VPay.Gateway.Common.UserManagement.Models;

namespace VPay.Gateway.Identity;

public interface IGetValidatedClientQuery
{
    Task<ValidateServiceResult?> GetUser(string clientId, string clientSecret);
}
