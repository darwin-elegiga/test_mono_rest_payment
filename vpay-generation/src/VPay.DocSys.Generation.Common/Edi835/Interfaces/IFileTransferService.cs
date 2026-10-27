using System;
using System.Threading;
using System.Threading.Tasks;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Provides methods to transfer files to a remote host.
    /// </summary>
    public interface IFileTransferService
    {
        /// <summary>
        /// Transfers a file to a remote host using the specified <paramref name="userName" /> and <paramref name="password" />.
        /// </summary>
        /// <param name="localPath">The local path to the file to be transferred.</param>
        /// <param name="remoteHost">The remote host to transfer the file to.</param>
        /// <param name="remotePath">The destination path on the remote host.</param>
        /// <param name="userName">The user name to use to connect to the remote host.</param>
        /// <param name="password">The password to use to connect to the remote host.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task TransferFile(string localPath, string remoteHost, string remotePath, string userName, string password, CancellationToken cancellationToken = default);
    }
}
