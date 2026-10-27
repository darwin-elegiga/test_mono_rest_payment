using Refit;
using VPay.Gateway.Common.UserManagement.Models;

namespace VPay.Gateway.Common.UserManagement;

public interface IUserManagementApi
{
    [Post("/api/v1/service-users/validate")]
    Task<ApiResponse<ValidateServiceResult>> ValidateUser(ValidateServiceRequest request, CancellationToken cancellationToken);
}
