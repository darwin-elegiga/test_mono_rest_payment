using System;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using VPay.DocSys.Common;

namespace VPay.Distribution.Common
{
    public class SshOperations : ISshOperations
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<SshOperations> _logger;
        private readonly ISftpClientFactory _sftpClientFactory;
        private readonly ISshClientFactory _sshClientFactory;
        private static readonly int _defaultPort = 22;
        private static readonly string _defaultPrivateKeyPath = "/app/.ssh/id_rsa";

        public SshOperations(
            IFileSystem fileSystem,
            ILogger<SshOperations> logger,
            ISftpClientFactory sftpClientFactory,
            ISshClientFactory sshClientFactory)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            _sftpClientFactory = sftpClientFactory;
            _sshClientFactory = sshClientFactory;
        }

        public async Task TransferFileAsync(string filePath, ScpSettings scpSettings)
        {
            ArgumentHelpers.ThrowIf.IsNull(filePath, nameof(filePath));
            ArgumentHelpers.ThrowIf.IsNull(scpSettings, nameof(scpSettings));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(scpSettings.Host, $"{nameof(scpSettings)}.{nameof(scpSettings.Host)}");
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(scpSettings.User, $"{nameof(scpSettings)}.{nameof(scpSettings.User)}");
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(scpSettings.Path, $"{nameof(scpSettings)}.{nameof(scpSettings.Path)}");

            using (_logger.BeginScope("{FilePath}", filePath))
            using (_logger.BeginScope(scpSettings.ToDictionary()))
            {
                if (scpSettings.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    _fileSystem.File.Copy(filePath, scpSettings.Path, true);
                }
                else
                {
                    _logger.LogInformation($"Transferring file [{filePath}] to [{scpSettings.Host}] using SSH.NET.");

                    try
                    {
                        using var sftp = _sftpClientFactory.Create(
                            scpSettings.Host,
                            _defaultPort,
                            scpSettings.User,
                            _defaultPrivateKeyPath
                        );

                        if (sftp is SftpClient sshNetSftp)
                        {
                            sshNetSftp.HostKeyReceived += (sender, e) => { e.CanTrust = true; };
                        }

                        sftp.Connect();

                        using var fileStream = _fileSystem.File.OpenRead(filePath);
                        //sftp.UploadFile(fileStream, scpSettings.Path, true);
                        var fileName = _fileSystem.Path.GetFileName(filePath);
                        sftp.UploadFile(fileStream, _fileSystem.Path.Combine(scpSettings.Path, fileName), true);

                        sftp.Disconnect();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error transferring file [{filePath}] to [{scpSettings.Host}]: {ex.Message}. Inner: {ex.InnerException?.Message ?? "N/A"}");
                        throw;
                    }
                }
            }
        }

        public async Task TouchControlFileOnServerAsync(string fileName, ScpSettings scpSettings)
        {
            ArgumentHelpers.ThrowIf.IsNull(fileName, nameof(fileName));
            ArgumentHelpers.ThrowIf.IsNull(scpSettings, nameof(scpSettings));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(scpSettings.Host, $"{nameof(scpSettings)}.{nameof(scpSettings.Host)}");
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(scpSettings.User, $"{nameof(scpSettings)}.{nameof(scpSettings.User)}");
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(scpSettings.Path, $"{nameof(scpSettings)}.{nameof(scpSettings.Path)}");

            var filePath = _fileSystem.Path.Combine(scpSettings.Path, fileName);

            using (_logger.BeginScope("{FilePath}", filePath))
            using (_logger.BeginScope(scpSettings.ToDictionary()))
            {
                _logger.LogInformation($"Creating control file [{filePath}] on [{scpSettings.Host}] using SSH.NET.");

                try
                {
                    using var ssh = _sshClientFactory.Create(
                        scpSettings.Host,
                        _defaultPort,
                        scpSettings.User,
                        _defaultPrivateKeyPath
                    );

                    ssh.Connect();

                    var command = $"touch {filePath}";
                    var result = ssh.RunCommand(command);

                    ssh.Disconnect();

                    if (result.ExitStatus != 0)
                    {
                        throw _logger.LogErrorAndReturnException<InvalidOperationException>(
                            $"Error creating control file [{filePath}] on [{scpSettings.Host}]: {result.Error}");
                    }
                }
                catch (Exception ex)
                {
                    // split logging and throwing to capture stack trace
                    _logger.LogError(ex, $"Error creating control file [{filePath}] on [{scpSettings.Host}]: {ex.Message}. Inner: {ex.InnerException?.Message ?? "N/A"}");
                    throw;
                }
            }
        }
    }
}
