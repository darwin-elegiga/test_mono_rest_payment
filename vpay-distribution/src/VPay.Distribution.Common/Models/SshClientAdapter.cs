using Renci.SshNet;

namespace VPay.Distribution.Common
{
    public class SshClientAdapter : ISshClient
    {
        private readonly SshClient _client;

        public SshClientAdapter(string host, int port, string user, string privateKeyPath)
        {
            var conn = new ConnectionInfo(host, port, user,
                new PrivateKeyAuthenticationMethod(user, new PrivateKeyFile(privateKeyPath)))
            {
                Timeout = System.TimeSpan.FromSeconds(60)
            };

            _client = new SshClient(conn);
        }

        public void Connect() => _client.Connect();

        public void Disconnect() => _client.Disconnect();

        public ISshCommand RunCommand(string command) => new SshCommandAdapter(_client.RunCommand(command));

        public void Dispose() => _client.Dispose();
    }
}
