using System;

namespace VPay.Distribution.Common
{
    public interface ISshClient : IDisposable
    {
        void Connect();
        void Disconnect();
        ISshCommand RunCommand(string command);
    }

    public interface ISshCommand
    {
        int ExitStatus { get; }
        string Error { get; }
    }

    public interface ISshClientFactory
    {
        ISshClient Create(string host, int port, string user, string privateKeyPath);
    }
}
