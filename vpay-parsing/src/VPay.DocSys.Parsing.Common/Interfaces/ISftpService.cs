using System.Threading;
using System.Threading.Tasks;

namespace VPay.DocSys.Parsing.Common
{
    /// <summary>
    /// Provides methods for transferring files to a remote host.
    /// </summary>
    public interface ISftpService
    {
        /// <summary>
        /// Transfers a file to a remote host using the specified <paramref name="userName"/> and the default RSA key file.
        /// </summary>
        /// <param name="localPath">The local path of the file to transfer.</param>
        /// <param name="remoteHost">The remote host to transfer the file to.</param>
        /// <param name="remotePath">The destination path on the remote host.</param>
        /// <param name="userName">The user name to use to connect to the remote host.</param>
        /// <param name="permissions">If given, the permissions to apply to the file on the remote host.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task TransferFile(string localPath, string remoteHost, string remotePath, string userName, short? permissions = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Transfers a file to a remote host using the specified <paramref name="userName" /> and <paramref name="password" />.
        /// </summary>
        /// <param name="localPath">The local path of the file to transfer.</param>
        /// <param name="remoteHost">The remote host to transfer the file to.</param>
        /// <param name="remotePath">The destination path on the remote host.</param>
        /// <param name="userName">The user name to use to connect to the remote host.</param>
        /// <param name="password">The password to use to connect to the remote host.</param>
        /// <param name="permissions">If given, the permissions to apply to the file on the remote host.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task TransferFile(string localPath, string remoteHost, string remotePath, string userName, string password, short? permissions = null, CancellationToken cancellationToken = default);
    }
}
