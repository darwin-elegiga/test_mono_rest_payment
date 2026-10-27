namespace VPay.Gateway.Management.Application;

public interface IUserInfo
{
    public string Email { get; }

    public string UserName { get; }

    public bool IsAuthenticated { get; }
}
