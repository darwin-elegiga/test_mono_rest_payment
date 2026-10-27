using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Renci.SshNet.Common;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Inbound835Files;
using VPay.DocSys.Data;
using VPay.Extensions.Ssh;

namespace VPay.DocSys.Parsing
{
    /// <summary>
    /// A base implementation of <see cref="IInbound835FileProcessor"/> from which specialized implementations can be derived.
    /// </summary>
    public abstract class Inbound835FileProcessor : IInbound835FileProcessor
    {
        #region [-- FileTransferParams --]

        protected class FileTransferParams
        {
            public string RemoteHost { get; set; }
            public bool UseRsaKey { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
            public string RemoteFilePath { get; set; }
        }

        #endregion

        private readonly IFileSystem _fileSystem;
        private readonly ISftpService _sftpService;
        private readonly IEdi835Repository _edi835Repository;
        protected readonly IInbound835FileParser _inbound835FileParser;
        private readonly ILogger _logger;

        protected Inbound835FileProcessor(
            IFileSystem fileSystem,
            ISftpService sftpService,
            IEdi835Repository edi835Repository,
            IInbound835FileParser inbound835FileParser,
            ILogger logger)
        {
            ArgumentHelpers.ThrowIf.IsNull(fileSystem, nameof(fileSystem));
            ArgumentHelpers.ThrowIf.IsNull(sftpService, nameof(sftpService));
            ArgumentHelpers.ThrowIf.IsNull(edi835Repository, nameof(edi835Repository));
            ArgumentHelpers.ThrowIf.IsNull(inbound835FileParser, nameof(inbound835FileParser));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _fileSystem = fileSystem;
            _sftpService = sftpService;
            _edi835Repository = edi835Repository;
            _inbound835FileParser = inbound835FileParser;
            _logger = logger;
        }

        public abstract Task ProcessFile(Inbound835FileMessage message, CancellationToken cancellationToken = default);

        protected IFileSystem FileSystem
        {
            get => _fileSystem;
        }

        protected async Task<AcknowledgmentFile> ParseAcknowledgmentFile(string path, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Parsing acknowledgment file: {path}");

                var ackFile = await _inbound835FileParser.ParseAcknowledgmentFile(path, cancellationToken);

                _logger.LogInformation($"Parsing acknowledgment file complete.");

                return ackFile;
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, $"Parsing acknowledgment file failed: {exception.Message}");
                throw;
            }
        }

        protected async Task<PaymentPostedFile> ParsePaymentPostedFile(string path, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Parsing payment posted file: {path}");

                var pmtPostedFile = await _inbound835FileParser.ParsePaymentPostedFile(path, cancellationToken);

                _logger.LogInformation($"Parsing payment posted file complete.");

                return pmtPostedFile;
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, $"Parsing payment posted file failed: {exception.Message}");
                throw;
            }
        }

        protected async Task<RemittanceAdviceFile> ParseRemittanceAdviceFile(string path, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Parsing remittance advice file: {path}");

                var remittanceAdviceFile = await _inbound835FileParser.ParseRemittanceAdviceFile(path, cancellationToken);

                _logger.LogInformation($"Parsing remittance advice file complete.");

                return remittanceAdviceFile;
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, $"Parsing remittance advice file failed: {exception.Message}");
                throw;
            }
        }

        protected async Task WritePaymentPostedFile(string path, IEnumerable<PaymentPostedFile.Line> lines, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Writing payment posted file: {path}");

                await _inbound835FileParser.WritePaymentPostedFile(path, lines, cancellationToken);

                _logger.LogInformation($"Writing payment posted file complete.");
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, $"Writing payment posted file failed: {exception.Message}");
                throw;
            }
        }

        protected async Task<Edi835> LookupEdi835(long transactionId, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Looking up 835 request for transaction: {transactionId}");

                var edi835 = await _edi835Repository.GetMostRecent835(transactionId, cancellationToken);

                _logger.LogInformation("Looking up 835 request for transaction complete.");

                return edi835;
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, $"Looking up 835 request for transaction failed: {exception.Message}");
                throw;
            }
        }

        protected async Task<IList<Edi835>> LookupEdi835s(IEnumerable<long> transactionIds, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Looking up 835s for {transactionIds.Count()} transactions.");

                var edi835s = await _edi835Repository.GetMostRecent835s(transactionIds, cancellationToken);

                _logger.LogInformation($"Looking up 835 requests for transactions complete - found {edi835s.Count} 835s.");

                return edi835s;
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, $"Looking up 835 requests for transactions failed: {exception.Message}");
                throw;
            }
        }

        protected async Task<IList<Edi835Status>> LookupEdi835StatusHistory(long edi835Id, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Looking up status history for 835 request id: {edi835Id}");

                var statusHistory = await _edi835Repository.Get835StatusHistory(edi835Id, cancellationToken);

                _logger.LogInformation("Looking up status history for 835 request complete.");

                return statusHistory;
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, $"Looking up status history for 835 request failed: {exception.Message}");
                throw;
            }
        }

        protected async Task Update835Status(Edi835 edi835, Edi835StatusCode statusCode, string associatedFileName, string comment, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Updating 835 request status: {statusCode}");

                await _edi835Repository.Add835Status(
                    edi835.Edi835Id,
                    statusCode,
                    comments: comment,
                    associatedFileName: associatedFileName,
                    createdByProgram: System.Reflection.Assembly.GetEntryAssembly().GetName().Name,
                    cancellationToken: cancellationToken);

                _logger.LogInformation($"Updating 835 request status completed.");
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, $"Updating 835 request status failed: {exception.Message}");
                throw;
            }
        }

        protected async Task Update835Statuses(IEnumerable<Edi835> edi835s, Edi835StatusCode statusCode, string associatedFileName, string comment, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Updating 835 request statuses: {statusCode}");

                await _edi835Repository.Add835Status(
                    edi835s.Select(_ => _.Edi835Id),
                    statusCode,
                    comments: comment,
                    associatedFileName: associatedFileName,
                    createdByProgram: System.Reflection.Assembly.GetEntryAssembly().GetName().Name,
                    cancellationToken: cancellationToken);

                _logger.LogInformation($"Updating 835 request statuses completed.");
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, $"Updating 835 request statuses failed: {exception.Message}");
                throw;
            }
        }

        protected Task TransferFileToRemotePath(string localPath, string remoteHost, string remotePath, string userName, short? permissions = null, CancellationToken cancellationToken = default)
        {
            return _sftpService.TransferFile(
                localPath: localPath,
                remoteHost: remoteHost,
                remotePath: remotePath,
                userName: userName,
                permissions: permissions,
                cancellationToken: cancellationToken);
        }

        protected Task TransferFileToRemotePath(string localPath, string remoteHost, string remotePath, string userName, string password, short? permissions = null, CancellationToken cancellationToken = default)
        {
            return _sftpService.TransferFile(
                localPath: localPath,
                remoteHost: remoteHost,
                remotePath: remotePath,
                userName: userName,
                password: password,
                permissions: permissions,
                cancellationToken: cancellationToken);
        }
    }
}
