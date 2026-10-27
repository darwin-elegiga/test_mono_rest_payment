namespace VPay.Distribution.Common
{
    public class SftpClientFactory : ISftpClientFactory
    {
        public ISftpClient Create(string host, int port, string user, string privateKeyPath)
            => new SftpClientAdapter(host, port, user, privateKeyPath);
    }
}
