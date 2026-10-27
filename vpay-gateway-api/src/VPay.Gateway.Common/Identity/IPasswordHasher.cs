namespace VPay.Gateway.Common.Identity;

public interface IPasswordHasher
{
    string Hash(string password);

    bool VerifyPassword(string hash, string password);
}
