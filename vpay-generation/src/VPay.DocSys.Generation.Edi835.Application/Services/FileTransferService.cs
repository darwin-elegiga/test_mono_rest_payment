using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.Extensions.Ssh;
using Polly;
using Renci.SshNet.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Edi835.Application
{
    public class FileTransferService : IFileTransferService
    {
        private readonly ISshClientFactory _sshClientFactory;
        private readonly ILogger<IFileTransferService> _logger;
        private readonly IAsyncPolicy _retryPolicy;

        public FileTransferService(
            ISshClientFactory sshClientFactory,
            ILogger<FileTransferService> logger)
        {
            _sshClientFactory = sshClientFactory;
            _logger = logger;

            _retryPolicy = Policy
                .Handle<SshConnectionException>()
                .WaitAndRetryAsync(
                    (2),
                    (r) => TimeSpan.FromSeconds(3),
                    OnRetry);
        }

        public async Task TransferFile(string localPath, string remoteHost, string remotePath, string userName, string password, CancellationToken cancellationToken = default)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(localPath, nameof(localPath));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(remoteHost, nameof(remoteHost));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(remotePath, nameof(remotePath));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(userName, nameof(userName));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(password, nameof(password));

            try
            {
                using(_logger.BeginScope(new { LocalPath = localPath, RemoteHost = remoteHost, RemotePath = remotePath, UserName = userName }.ToDictionary()))
                {
                    _logger.LogInformation($"Transferring file to remote host: {remoteHost}:{remotePath}");

                    await _retryPolicy.ExecuteAsync(async () =>
                    {
                        using(var sftpClient = _sshClientFactory.CreateSftpClient(remoteHost, userName, password))
                        {
                            sftpClient.Connect();

                            try
                            {
                                await sftpClient.UploadFileAsync(localPath, remotePath, true, cancellationToken);
                            }
                            finally
                            {
                                sftpClient.Disconnect();
                            }
                        }

                    }, "Transferring file to remote host");

                    _logger.LogInformation($"Transferring file to remote host complete.");
                }
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, $"Transferring file to remote host failed: {exception.Message}");
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
