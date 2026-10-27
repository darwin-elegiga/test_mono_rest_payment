using System;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Inbound835Files;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Parsing
{
    /// <summary>
    /// An <see cref="IInbound835FileProcessor"/> that processes <see cref="Inbound835FileType.RemittanceAdvice"/> files.
    /// </summary>
    public class Inbound835RemittanceAdviceFileProcessor : Inbound835FileProcessor, IInbound835RemittanceAdviceFileProcessor
    {
        private readonly IPlatformConfigurationApi _platformConfigurationApi;
        private readonly RemittanceAdviceFileSettings _remittanceAdviceFileSettings;
        private readonly IStorageManager _storageManager;
        private readonly ILogger<Inbound835RemittanceAdviceFileProcessor> _logger;

        public Inbound835RemittanceAdviceFileProcessor(
            IFileSystem fileSystem,
            ISftpService sftpService,
            IEdi835Repository edi835Repository,
            IInbound835FileParser inbound835FileParser,
            IPlatformConfigurationApi platformConfigurationApi,
            Inbound835FileServiceSettings inbound835FileServiceSettings,
            IStorageManager storageManager,
            ILogger<Inbound835RemittanceAdviceFileProcessor> logger
            ) : base(fileSystem, sftpService, edi835Repository, inbound835FileParser, logger)
        {
            _platformConfigurationApi = platformConfigurationApi;
            _remittanceAdviceFileSettings = inbound835FileServiceSettings.RemittanceAdviceFile;
            _storageManager = storageManager;
            _logger = logger;
        }

        public override async Task ProcessFile(Inbound835FileMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentHelpers.ThrowIf.IsNull(message, nameof(message));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(message.FilePath, nameof(message), nameof(message.FilePath));

            using(_logger.BeginScope(message, _remittanceAdviceFileSettings))
            {
                var remittanceAdviceFile = await ParseRemittanceAdviceFile(message.FilePath, cancellationToken);

                // start new log scope with transaction id
                using(_logger.BeginScope(new { remittanceAdviceFile.TransactionId }.ToDictionary()))
                {
                    // get 835 from docsys database
                    var edi835 = await LookupEdi835(remittanceAdviceFile.TransactionId, cancellationToken);

                    // if no 835 is found it's an error condition
                    if(edi835 == null)
                    {
                        throw _logger.LogErrorAndReturnException($"No 835 request found for transaction: {remittanceAdviceFile.TransactionId}");
                    }

                    // start new log scope with edi835 id
                    using(_logger.BeginScope(new { edi835.Edi835Id }.ToDictionary()))
                    {
                        // log that we found the 835...
                        _logger.LogInformation($"Found 835 request: {edi835.Edi835Id}");

                        // Get the File Type
                        var fileType = _inbound835FileParser.GetFileType(message.FilePath);

                        // Determine if it is a .brn file
                        bool isBrn = fileType == Inbound835FileType.BlueRemittanceNotification;

                        // ...process the file
                        await ProcessRemittanceAdviceFile(remittanceAdviceFile, edi835, isBrn, cancellationToken);
                    }
                }
            }
        }

        public async Task Send835ToAvaility(long transactionId, CancellationToken cancellationToken)
        {
            using(_logger.BeginScope(new { TransactionId = transactionId }.ToDictionary()))
            {
                try
                {
                    _logger.LogInformation($"Sending 835 to Availity for transaction id: {transactionId}");

                    // get the 835 for the transaction id
                    var edi835 = await LookupEdi835(transactionId, cancellationToken);

                    // if no 835 request, it's an error condition
                    if(edi835 == null)
                    {
                        throw new InvalidOperationException($"No 835 request found for transaction id: {transactionId}");
                    }

                    // check if the TPA/BillingEntity is configured for SendToAvaility
                    if(!await ShouldSendToAvaility(edi835, EdiDeliveryFileExtension.Edi835, cancellationToken))
                    {
                        throw new InvalidOperationException($"TPA/BE not configured for sending to Availity.  TPA: {edi835.ClientCode}, BE: {edi835.BillingEntityCode}");
                    }

                    // get all status' for the 835 request
                    var statusHistory = await LookupEdi835StatusHistory(edi835.Edi835Id, cancellationToken);

                    // find the most recent Received835FromVendor status
                    var recvd835Status = statusHistory.LastOrDefault(s => s.StatusCode == Edi835StatusCode.Received835FromVendor);

                    // if no Received835FromVendor status record, its an error condition
                    if(recvd835Status == null)
                    {
                        throw new InvalidOperationException($"The 835 for transaction id {transactionId} has not been received from vendor.");
                    }

                    // if the file path is null or empty, its an error condition
                    if(string.IsNullOrEmpty(recvd835Status.AssociatedFileName))
                    {
                        throw new InvalidOperationException($"The 835 request status record for transaction id {transactionId} does not contain the file name of the associated 835 file.");
                    }

                    // the path to the 835 file is the file associated with the Received835FromVendor status
                    var pathTo835File = recvd835Status.AssociatedFileName;

                    // parse the 835 document
                    var remittanceAdviceFile = await ParseRemittanceAdviceFile(pathTo835File, cancellationToken);

                    // send it to availity
                    await SendToAvaility(remittanceAdviceFile, cancellationToken);

                    _logger.LogInformation($"Sending 835 to Availity completed.");
                }
                catch(Exception exception)
                {
                    _logger.LogError(exception, $"Sending 835 to Availity failed: {exception.Message}");
                    throw;
                }
            }
        }

        public async Task SendBrnToAvaility(long transactionId, CancellationToken cancellationToken)
        {
            using (_logger.BeginScope(new { TransactionId = transactionId }.ToDictionary()))
            {
                try
                {
                    _logger.LogInformation($"Sending BRN to Availity for transaction id: {transactionId}");

                    // get the 835 for the transaction id
                    var edi835 = await LookupEdi835(transactionId, cancellationToken);

                    // if no 835 request, it's an error condition
                    if (edi835 == null)
                    {
                        throw new InvalidOperationException($"No 835 request found for transaction id: {transactionId}");
                    }

                    // check if the TPA/BillingEntity is configured for SendToAvaility
                    if (!await ShouldSendToAvaility(edi835, EdiDeliveryFileExtension.EdiBRN, cancellationToken))
                    {
                        throw new InvalidOperationException($"TPA/BE not configured for sending to Availity. TPA: {edi835.ClientCode}, BE: {edi835.BillingEntityCode}");
                    }

                    // get all status' for the 835 request
                    var statusHistory = await LookupEdi835StatusHistory(edi835.Edi835Id, cancellationToken);

                    // find the most recent Received835FromVendor status
                    var recvdBrnStatus = statusHistory.LastOrDefault(s => s.StatusCode == Edi835StatusCode.ReceivedBRNFromVendor);

                    // if no Received835FromVendor status record, its an error condition
                    if (recvdBrnStatus == null)
                    {
                        throw new InvalidOperationException($"The BRN for transaction id {transactionId} has not been received from vendor.");
                    }

                    // if the file path is null or empty, its an error condition
                    if (string.IsNullOrEmpty(recvdBrnStatus.AssociatedFileName))
                    {
                        throw new InvalidOperationException($"The 835 request status record for transaction id {transactionId} does not contain the file name of the associated BRN file.");
                    }

                    // the path to the BRN file is the file associated with the ReceivedBrnFromVendor status
                    var pathToBrnFile = recvdBrnStatus.AssociatedFileName;

                    // parse the BRN document
                    var remittanceFile = await ParseRemittanceAdviceFile(pathToBrnFile, cancellationToken);

                    // send it to availity
                    await SendToAvaility(remittanceFile, cancellationToken);

                    _logger.LogInformation($"Sending BRN to Availity completed.");
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, $"Sending BRN to Availity failed: {exception.Message}");
                    throw;
                }
            }
        }

        private async Task ProcessRemittanceAdviceFile(RemittanceAdviceFile remittanceAdviceFile, Edi835 edi835, bool isBrn, CancellationToken cancellationToken)
        {
            // Set up enums based on file type
            var (receivedStatusCode, ediDeliveryFileExtension, sentStatusCode) = isBrn
                ? (Edi835StatusCode.ReceivedBRNFromVendor, EdiDeliveryFileExtension.EdiBRN, Edi835StatusCode.SentBRNToAvaility)
                : (Edi835StatusCode.Received835FromVendor, EdiDeliveryFileExtension.Edi835, Edi835StatusCode.Sent835ToAvaility);

            // get archive file for transaction
            var archiveFile = await GetArchiveFileForTransaction(remittanceAdviceFile.TransactionId, cancellationToken);

            // if no archive exists for the transaction, its an error condition
            if(archiveFile == null)
            {
                throw _logger.LogErrorAndReturnException($"No archive found for transaction id: {remittanceAdviceFile.TransactionId}");
            }

            // create new logging scope with archive file id
            using(_logger.BeginScope(new { archiveFile.ArchiveFileId }.ToDictionary()))
            {
                // determine if the archive already contains this 835 document
                var docAlreadyArchived = _storageManager.GetArchiveFileDetailNames(archiveFile).Contains(remittanceAdviceFile.FileName);

                // if 835 already in archive...
                if(docAlreadyArchived)
                {
                    // log that it was already in archive
                    _logger.LogInformation("Archive already contains 835/brn document: {RemittanceAdviceFileName}", remittanceAdviceFile.FileName);
                }
                else
                {
                    // otherwise, add 835 doc to archive
                    await AddRemittanceAdviceFileToNewDocumentArchive(remittanceAdviceFile, archiveFile, isBrn);
                }

                // Get the edi835 status history
                var statusHistory = await LookupEdi835StatusHistory(edi835.Edi835Id, cancellationToken);

                // Check the status history for the relevant 'received' status
                var receivedStatus = statusHistory.LastOrDefault(x => x.StatusCode == receivedStatusCode);

                // if 835 was not already in archive or the 835 status not present
                if (!docAlreadyArchived || receivedStatus == null)
                {
                    // update 835 status to Received835FromVendor
                    await Update835Status(edi835, receivedStatusCode, remittanceAdviceFile.FullFileName, remittanceAdviceFile.Contents, cancellationToken);
                }


                // if the transaction is one that the 835 should be sent to availity
                if (await ShouldSendToAvaility(edi835, ediDeliveryFileExtension, cancellationToken))
                {
                    // if sending to availity is enabled
                    if(_remittanceAdviceFileSettings.SendToAvaility.Enabled)
                    {
                        // send to availity
                        await SendToAvaility(remittanceAdviceFile, cancellationToken);
                    }
                    else
                    {
                        // otherwise, log that its disabled
                        _logger.LogInformation("Send to Availity disabled - skipping file transfer.");
                    }

                    // Check the status history for the relevant 'sent' status
                    var sentStatus = statusHistory.LastOrDefault(x => x.StatusCode == sentStatusCode);

                    // if 835 or brn was not already sent or the current 835 status < Send835ToAvaility
                    if (sentStatus == null)
                    {
                        // update 835 status to Sent835ToAvaility
                        await Update835Status(edi835, sentStatusCode, remittanceAdviceFile.FullFileName, remittanceAdviceFile.Contents, cancellationToken);
                    }
                }
                else
                {
                    _logger.LogInformation("Send to Availity disabled for {ClientCode}/{BillingEntityCode} - skipping file transfer and status update",
                        edi835.ClientCode, edi835.BillingEntityCode);
                }
            }
        }

        private Task<bool> ShouldSendToAvaility(Edi835 edi835, EdiDeliveryFileExtension fileExtension, CancellationToken cancellationToken)
            => _platformConfigurationApi
                .IsEdiDeliveryEnabledForClient(edi835.ClientCode, edi835.BillingEntityCode, EdiDeliveryDestination.Availity, fileExtension, StringComparison.OrdinalIgnoreCase, cancellationToken);

        private async Task SendToAvaility(RemittanceAdviceFile remittanceAdviceFile, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Sending 835/brn to Availity : {remittanceAdviceFile.FullFileName}");

                var fileTransferParams = GetSendToAvailityTransferParams(remittanceAdviceFile);

                if(fileTransferParams.UseRsaKey)
                {
                    await TransferFileToRemotePath(
                        localPath: remittanceAdviceFile.FullFileName,
                        remoteHost: fileTransferParams.RemoteHost,
                        remotePath: fileTransferParams.RemoteFilePath,
                        userName: fileTransferParams.UserName,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await TransferFileToRemotePath(
                        localPath: remittanceAdviceFile.FullFileName,
                        remoteHost: fileTransferParams.RemoteHost,
                        remotePath: fileTransferParams.RemoteFilePath,
                        userName: fileTransferParams.UserName,
                        password: fileTransferParams.Password,
                        cancellationToken: cancellationToken);
                }

                _logger.LogInformation($"Sending 835/brn to Availity  completed.");
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, $"Sending 835/brn to Availity failed: {exception.Message}");
                throw;
            }
        }

        private FileTransferParams GetSendToAvailityTransferParams(RemittanceAdviceFile remittanceAdviceFile)
        {
            var transferSettings = _remittanceAdviceFileSettings.SendToAvaility.TransferSettings;

            return new FileTransferParams()
            {
                RemoteHost = transferSettings.Host,
                UseRsaKey = transferSettings.UseRsaKey,
                UserName = transferSettings.UserName,
                Password = transferSettings.Password,
                RemoteFilePath = FileSystem.Path.Combine(transferSettings.TransferPath, $"AVAILT_DOC835_{remittanceAdviceFile.FileName}"),
            };
        }

        private async Task<IArchiveFile> GetArchiveFileForTransaction(long transactionId, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Finding document archive for transaction: {transactionId}");

                var archiveFile = (await _storageManager.GetArchivesForTransactionId(transactionId)).OrderBy(_ => _.ArchiveFileId).FirstOrDefault();

                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation("Finding document archive complete.");

                return archiveFile;
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, $"Finding document archive failed: {exception.Message}");
                throw;
            }
        }

        [Obsolete("Method adds file to an existing .zip file. Use AddRemittanceAdviceFileToNewDocumentArchive instead.")]
        private async Task AppendRemittanceAdviceFileToDocumentArchive(RemittanceAdviceFile remittanceAdviceFile, IArchiveFile archiveFile, CancellationToken cancellationToken)
        {
            try
            {
                using(_logger.BeginScope(new { archiveFile.ArchiveFileId }.ToDictionary()))
                {
                    if(_storageManager.GetArchiveFileDetailNames(archiveFile).Contains(remittanceAdviceFile.FileName))
                    {
                        _logger.LogInformation($"Archive already contains 835 document: {remittanceAdviceFile.FileName}");
                    }
                    else
                    {
                        _logger.LogInformation($"Adding 835 to document archive: {archiveFile.ArchiveFileId} for transaction: {remittanceAdviceFile.TransactionId}");

                        var archiveDetail = new ArchiveDetail()
                        {
                            ArchiveFileId = archiveFile.ArchiveFileId,
                            CreateDateTime = DateTime.Now,
                            CreateLogin = System.Reflection.Assembly.GetEntryAssembly().GetName().Name,
                            Description = "835 Document",
                            FileDetailType = VPay.DocSys.Common.FileDetailType.Text,
                            ModifyDateTime = DateTime.Now,
                            TransactionId = remittanceAdviceFile.TransactionId,
                            FileName = remittanceAdviceFile.FileName,
                            Data = FileSystem.File.ReadAllBytes(remittanceAdviceFile.FullFileName),
                        };

                        await _storageManager.AddDetailToArchive(archiveDetail);

                        _logger.LogInformation($"Adding 835 to document archive completed.");
                    }
                }
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, $"Adding 835 to document archive failed: {exception.Message}");
                throw;
            }
        }

        private async Task AddRemittanceAdviceFileToNewDocumentArchive(RemittanceAdviceFile remittanceAdviceFile, IArchiveFile archiveFile, bool isBrn)
        {
            try
            {
                using (_logger.BeginScope(new { archiveFile.ArchiveFileId }.ToDictionary()))
                {
                    if (_storageManager.GetArchiveFileDetailNames(archiveFile).Contains(remittanceAdviceFile.FileName))
                    {
                        _logger.LogInformation($"Archive already contains 835/brn document: {remittanceAdviceFile.FileName}");
                    }
                    else
                    {
                        _logger.LogInformation($"Adding new document archive with remittance advice file for transaction {remittanceAdviceFile.TransactionId}");

                        var newArchiveDetail = FileSystem.FileInfo.New(remittanceAdviceFile.FullFileName);
                        using var newArchiveDetailStream = newArchiveDetail.OpenRead();
                        var newArchive = await _storageManager.CreateArchive(
                            archiveFile: new ArchiveFile
                            {
                                StageFileKey = archiveFile.StageFileKey,
                                CreateLogin = archiveFile.CreateLogin,
                                Tpa = remittanceAdviceFile.ClientCode,
                                TransactionId = remittanceAdviceFile.TransactionId,
                                DocumentUniqueId = archiveFile.DocumentUniqueId,
                                ReferenceCode = remittanceAdviceFile.ClientCode,
                                DocumentSystemType = DocumentSystemType.Transaction,
                            },
                            generatedFiles: new System.Collections.Generic.List<IGeneratedFile>
                            {
                                new Inbound835RemittanceAdviceGeneratedFileStream
                                {
                                    TransactionId = remittanceAdviceFile.TransactionId.ToString(),
                                    FileName = remittanceAdviceFile.FileName,
                                    Description = isBrn ? "BRN Document" : "835 Document",
                                    FileDetailType = FileDetailType.Text,
                                    PageCount = 1,
                                    Length = newArchiveDetail.Length,
                                    Stream = newArchiveDetailStream,
                                }
                            },
                            cleanUp: false);

                        using (_logger.BeginScope(new { NewArchiveFileId = newArchive }))
                        {
                            _logger.LogInformation($"Added new document archive with remittance advice file completed.");
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Adding new document archive with remittance advice file failed: {exception.Message}");
                throw;
            }
        }
    }
}
