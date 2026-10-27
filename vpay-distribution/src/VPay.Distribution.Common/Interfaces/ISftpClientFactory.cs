namespace VPay.Distribution.Common
{
    public interface ISftpClientFactory
    {
        ISftpClient Create(string host, int port, string user, string privateKeyPath);
    }
}
