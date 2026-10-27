using System.Security.Claims;

namespace VPay.Gateway.Application;

public class UserInfo
{
    public UserInfo(ClaimsPrincipal user)
    {
        user = user ?? throw new ArgumentNullException(nameof(user));

        IsAuthenticated = user.Identity?.IsAuthenticated ?? false;
        OriginCode = user.FindFirst(ClaimTypes.System)?.Value ?? "";
        ClientCode = user.FindFirst(ClaimTypes.Name)?.Value ?? "";
        ClientId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
    }

    public bool IsAuthenticated { get; }

    public string OriginCode { get; }

    public string ClientCode { get; }

    public string ClientId { get; }
}
