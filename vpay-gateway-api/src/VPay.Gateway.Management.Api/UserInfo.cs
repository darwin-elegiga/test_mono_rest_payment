using System.Security.Claims;
using VPay.Gateway.Management.Application;

namespace VPay.Gateway.Management.Api;

public class UserInfo : IUserInfo
{
    private readonly IHttpContextAccessor _accessor;

    public UserInfo(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string Email => _accessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public string UserName => !string.IsNullOrWhiteSpace(ClaimName) ? ClaimName : CombinedName.Trim();

    public bool IsAuthenticated => _accessor.HttpContext.User.Identity.IsAuthenticated;

    private string FirstName => _accessor.HttpContext.User.FindFirstValue(ClaimTypes.GivenName) ?? "";
    private string LastName => _accessor.HttpContext.User.FindFirstValue(ClaimTypes.Surname) ?? "";
    private string ClaimName => _accessor.HttpContext.User.FindFirstValue(ClaimTypes.Name) ?? "";
    private string CombinedName => $"{FirstName} {LastName}";
}
