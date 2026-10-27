using Renci.SshNet;

namespace VPay.Distribution.Common
{
    public class SshCommandAdapter : ISshCommand
    {
        private readonly SshCommand _command;
        public SshCommandAdapter(SshCommand command) => _command = command;
        public int ExitStatus => _command.ExitStatus;
        public string Error => _command.Error;
    }
}
