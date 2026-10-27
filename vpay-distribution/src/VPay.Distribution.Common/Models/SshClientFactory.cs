namespace VPay.Distribution.Common
{
    public class SshClientFactory : ISshClientFactory
    {
        public ISshClient Create(string host, int port, string user, string privateKeyPath)
            => new SshClientAdapter(host, port, user, privateKeyPath);
    }
}
