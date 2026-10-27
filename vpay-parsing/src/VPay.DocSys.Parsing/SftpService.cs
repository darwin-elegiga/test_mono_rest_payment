using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Renci.SshNet.Common;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.Extensions.Ssh;
using Polly;

namespace VPay.DocSys.Parsing
{
    /// <summary>
    /// Provides methods for transferring files to a remote host.
    /// </summary>
    public class SftpService : ISftpService
    {
        public const string DefaultRsaFilePath = @"/ssh/id_rsa";

        private readonly IFileSystem _fileSystem;
        private readonly ISshClientFactory _sshClientFactory;
        private readonly ILogger<SftpService> _logger;
        private readonly IAsyncPolicy _retryPolicy;

        public SftpService(
            IFileSystem fileSystem,
            ISshClientFactory sshClientFactory,
            ILogger<SftpService> logger)
        {
            _fileSystem = fileSystem;
            _sshClientFactory = sshClientFactory;
            _logger = logger;

            // create retry policy
            _retryPolicy = Policy
                .Handle<SshConnectionException>()
                .WaitAndRetryAsync(
                    (2),
                    (r) => TimeSpan.FromSeconds(3),
                    OnRetry);
        }

        /// <inheritdoc/>
        public async Task TransferFile(string localPath, string remoteHost, string remotePath, string userName, short? permissions = null, CancellationToken cancellationToken = default)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(localPath, nameof(localPath));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(remoteHost, nameof(remoteHost));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(remotePath, nameof(remotePath));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(userName, nameof(userName));

            try
            {
                using(_logger.BeginScope(new { LocalPath = localPath, RemoteHost = remoteHost, RemotePath = remotePath, UserName = userName, Permissions = permissions ?? 0 }.ToDictionary()))
                {
                    _logger.LogInformation($"Transferring file to remote path: {remoteHost}:{remotePath}");

                    using(var rsaFileStream = _fileSystem.File.OpenRead(DefaultRsaFilePath))
                    {
                        await _retryPolicy.ExecuteAsync(async () =>
                        {
                            using(var sftpClient = _sshClientFactory.CreateSftpClient(remoteHost, userName, new PrivateKey(rsaFileStream)))
                            {
                                sftpClient.Connect();

                                try
                                {
                                    await sftpClient.UploadFileAsync(localPath, remotePath, true, cancellationToken);

                                    if(permissions.HasValue)
                                    {
                                        sftpClient.ChangePermissions(remotePath, permissions.Value);
                                    }
                                }
                                finally
                                {
                                    sftpClient.Disconnect();
                                }
                            }

                        }, "Transferring file to remote path");
                    }

                    _logger.LogInformation($"Transferring file to remote path completed.");
                }
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, $"Transferring file to remote path failed: {exception.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task TransferFile(string localPath, string remoteHost, string remotePath, string userName, string password, short? permissions = null, CancellationToken cancellationToken = default)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(localPath, nameof(localPath));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(remoteHost, nameof(remoteHost));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(remotePath, nameof(remotePath));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(userName, nameof(userName));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(password, nameof(password));

            try
            {
                using(_logger.BeginScope(new { LocalPath = localPath, RemoteHost = remoteHost, RemotePath = remotePath, UserName = userName, Permissions = permissions ?? 0 }.ToDictionary()))
                {
                    _logger.LogInformation($"Transferring file to remote path: {remoteHost}:{remotePath}");

                    await _retryPolicy.ExecuteAsync(async () =>
                    {
                        using(var sftpClient = _sshClientFactory.CreateSftpClient(remoteHost, userName, password))
                        {
                            sftpClient.Connect();

                            try
                            {
                                await sftpClient.UploadFileAsync(localPath, remotePath, true, cancellationToken);

                                if(permissions.HasValue)
                                {
                                    sftpClient.ChangePermissions(remotePath, permissions.Value);
                                }
                            }
                            finally
                            {
                                sftpClient.Disconnect();
                            }
                        }

                    }, "Transferring file to remote path");

                    _logger.LogInformation($"Transferring file to remote path completed.");
                }
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, $"Transferring file to remote path failed: {exception.Message}");
                throw;
            }
        }

        private void OnRetry(Exception exception, TimeSpan sleepDuration, int retryCount, Context context)
        {
            var operation = context.OperationKey ?? "(unknown operation)";

            using(_logger.BeginScope(new Dictionary<string, object>()
            {
                { "RetryCount", retryCount},
                { "SleepDuration", sleepDuration },
                { "Operation", operation },
            }))
            {
                _logger.LogInformation(exception, $"Retry ({retryCount}): {operation}");
            }
        }
    }
}
