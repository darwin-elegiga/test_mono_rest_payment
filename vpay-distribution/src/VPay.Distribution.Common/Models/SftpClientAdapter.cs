using System.IO;
using Renci.SshNet;

namespace VPay.Distribution.Common
{
    public class SftpClientAdapter : ISftpClient
    {
        private readonly SftpClient _client;

        public SftpClientAdapter(string host, int port, string user, string privateKeyPath)
        {
            var conn = new ConnectionInfo(host, port, user,
                new PrivateKeyAuthenticationMethod(user, new PrivateKeyFile(privateKeyPath)))
            {
                Timeout = System.TimeSpan.FromSeconds(60)
            };

            _client = new SftpClient(conn);
        }

        public void Connect() => _client.Connect();

        public void Disconnect() => _client.Disconnect();

        public void UploadFile(Stream input, string path, bool canOverride) => _client.UploadFile(input, path, canOverride);

        public void Dispose() => _client.Dispose();
    }
}
