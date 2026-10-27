using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.DocSys.Parsing.Common.Rabbitmq;
using VPay.DocSys.Storage;
using VPay.Queuing;

namespace VPay.DocSys.Parsing
{
    public class InventoryDriverFileService : IInventoryDriverFileService, IDriverFileService
    {
        private readonly IConfiguration _configuration;
        private readonly IFileSystem _fileSystem;
        private readonly NewDocSysArchive _newDocSysArchive;
        private readonly ILogger<InventoryDriverFileService> _logger;
        private readonly ILogger<IMessagePublisher> _loggerMessagePub;
        private readonly ITarFileService _tarFileService;
        private readonly IPdfService _pdfService;
        private readonly IRedCardRepository _redCardRepository;
        private readonly IDriverFileService _driverFileService;
        private readonly IStorageManager _storageManager;

        public InventoryDriverFileService(
            IConfiguration configuration,
            IRedCardRepository redCardSqlService,
            IFileSystem fileSystem,
            ITarFileService tarFileService,
            IPdfService pdfService,
            NewDocSysArchive newDocSysArchive,
            IDriverFileService driverFileService,
            IStorageManager storageManager,
            ILoggerFactory loggerFactory)
        {
            ArgumentHelpers.ThrowIf.IsNull(configuration, nameof(configuration));
            ArgumentHelpers.ThrowIf.IsNull(redCardSqlService, nameof(redCardSqlService));
            ArgumentHelpers.ThrowIf.IsNull(fileSystem, nameof(fileSystem));
            ArgumentHelpers.ThrowIf.IsNull(newDocSysArchive, nameof(newDocSysArchive));
            ArgumentHelpers.ThrowIf.IsNull(tarFileService, nameof(tarFileService));
            ArgumentHelpers.ThrowIf.IsNull(pdfService, nameof(pdfService));
            ArgumentHelpers.ThrowIf.IsNull(driverFileService, nameof(driverFileService));
            ArgumentHelpers.ThrowIf.IsNull(storageManager, nameof(storageManager));

            _configuration = configuration;
            _redCardRepository = redCardSqlService;
            _newDocSysArchive = newDocSysArchive;
            _fileSystem = fileSystem;
            _tarFileService = tarFileService;
            _pdfService = pdfService;
            _driverFileService = driverFileService;
            _storageManager = storageManager;
            _logger = loggerFactory.CreateLogger<InventoryDriverFileService>();
            _loggerMessagePub = loggerFactory.CreateLogger<IMessagePublisher>();

            this.IgnoreMissingTransactions = configuration.GetSection("InventoryDriverFileService").GetValue("IgnoreMissingTransactions", this.IgnoreMissingTransactions);
        }

        public bool IgnoreMissingTransactions { get; set; }

        public async Task<RedCardProcessResult> ParseFile(string inventoryDriverFileName)
        {
            using (_logger.BeginScope(new Dictionary<string, object>()
            {
                {"InventoryDriverFileName", inventoryDriverFileName}
            }))
            {
                if (!_driverFileService.GetLock(inventoryDriverFileName))
                {
                    _logger.LogInformation($"File has already been processed or is currently being processed by another consumer.");
                    return new RedCardProcessResult();
                }

                try
                {
                    ArgumentHelpers.ThrowIf.IsNullOrEmpty(inventoryDriverFileName, nameof(inventoryDriverFileName));

                    _logger.LogInformation($"Start Processing {inventoryDriverFileName.Replace(Environment.NewLine, "")}");

                    if (!_fileSystem.File.Exists(inventoryDriverFileName))
                    {
                        return new RedCardProcessResult(RedCardProcessResultCode.Error, $"Driver file not found: {inventoryDriverFileName}");
                    }

                    var tarFilePath = GetTarFilePath(inventoryDriverFileName);

                    if (string.IsNullOrEmpty(tarFilePath))
                    {
                        return new RedCardProcessResult(RedCardProcessResultCode.Retry, $"Processed tar control file {tarFilePath} not found for {inventoryDriverFileName}");
                    }

                    if(!_fileSystem.File.Exists(tarFilePath))
                    {
                        return new RedCardProcessResult(RedCardProcessResultCode.Error, $"Tar file not found: {tarFilePath}");
                    }

                    var inventoryDriverFile = new InventoryDriverFile(_fileSystem.File.ReadLines(inventoryDriverFileName).ToArray());

                    if (inventoryDriverFile.Records.Count <= 0)
                    {
                        _logger.LogWarning($"InventoryDriver contains no records.");
                        return new RedCardProcessResult();
                    }

                    var result = await ProcessInventoryDriver(
                        inventoryDriverFile,
                        inventoryDriverFileName,
                        tarFilePath).ConfigureAwait(false);

                    if (result.ResultCode == RedCardProcessResultCode.Error)
                    {
                        _logger.LogError($"An error occurred processing Inventory Driver File: {result.Description}");
                    }
                    else if (result.ResultCode == RedCardProcessResultCode.Retry)
                    {
                        _logger.LogInformation($"Unable to process Inventory Driver File now ({result.Description}) - try again later.");
                    }
                    else
                    {
                        _logger.LogInformation("Completed Processing {FilePath}", inventoryDriverFileName.Replace(Environment.NewLine, ""));

                        _driverFileService.WriteLogFile(inventoryDriverFileName, $"{result.RecordsProcessed} records processed");

                        foreach (var f in result.FileNames)
                        {
                            _logger.LogInformation($"InventoryDriver File {f} created");
                        }
                        foreach (var f in result.ControlFileNames)
                        {
                            _logger.LogInformation($"InventoryDriver Control File {f} created");
                        }
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unexpected exception occurred");
                    return new RedCardProcessResult(RedCardProcessResultCode.Error, ex.Message);
                }
                finally
                {
                    _driverFileService.ReleaseLock(inventoryDriverFileName);
                }
            }
        }

        public Task QueueFileForParsing(string inventoryDriverFileName)
        {
            var queueSettings = new QueueConfigurationSettings();
            _configuration.Bind("InventoryDriverFileQueue", queueSettings);

            var msg = new RedCardParsingMessage
            {
                FilePath = inventoryDriverFileName
            };

            using (var msgQueue = QuorumPublisherFactory.GetPublisher(queueSettings, _loggerMessagePub))
            {
                msgQueue.Publish(JsonConvert.SerializeObject(msg));
                _logger.LogDebug($"Queued inventoryDriverFilePath={msg.FilePath.Replace(Environment.NewLine, "")}");
            }

            return Task.CompletedTask;
        }

        private string GetTarFilePath(string inventoryDriverFilePath)
        {
            var ctrlFilePath = _fileSystem.Path.Combine(
                _newDocSysArchive.Path,
                _fileSystem.Path
                    .GetFileName(inventoryDriverFilePath)
                    .GetTarFileName() +
                ".ctrl.proc");

            if (!_fileSystem.File.Exists(ctrlFilePath))
                return null;

            var lines = _fileSystem.File.ReadAllLines(ctrlFilePath);
            if (lines.Length == 0)
                return null;

            return lines[0].TrimEnd('\r', '\n', '\t', ' ');
        }

        private async Task<RedCardProcessResult> ProcessInventoryDriver(InventoryDriverFile inventoryDriverFile, string inventoryDriverFilePath, string tarFilePath)
        {
            // get unique transaction ids from driver file
            var transactionIds = inventoryDriverFile.Records.Select(r => r.TransactionID).Distinct().ToList();

            // get DocumentTransaction records from the MS SQL database for each transaction id
            var documentTransactions = await _redCardRepository.GetDocumentTransactions(transactionIds).ConfigureAwait(false);


            // if any transactions are missing from the MS SQL database...
            if (documentTransactions.Count < transactionIds.Count)
            {
                if (this.IgnoreMissingTransactions)
                {
                    // remove all records from the driver file that aren't in docsys
                    var docSysTransactionIds = documentTransactions.Select(t => t.TransactionID).ToList();
                    inventoryDriverFile.Records.RemoveAll(r => !docSysTransactionIds.Contains(r.TransactionID));
                }
                else
                {
                    // bail out with a Retry result code because this means we haven't processed the .sts for one or more of the transactions in the inv.drv file
                    var missingTransactionIds = transactionIds.Except(documentTransactions.Select(t => t.TransactionID));
                    return new RedCardProcessResult(RedCardProcessResultCode.Retry, $"The following transactions from the driver file were not found - {string.Join(", ", missingTransactionIds.Select(t => t.ToString()))}");
                }
            }

            // if any transactions have a status of StapleDocsMsgQueued, bail out with a Retry result code because this means those transactions are already being processed
            var queuedTransactions = documentTransactions.Where(t => t.StatusCode == RedCardTransactionStatusCode.StapleDocumentsMessageQueued).ToList();
            if (queuedTransactions.Any())
            {
                var queuedTransactionIds = queuedTransactions.Select(t => t.TransactionID);
                return new RedCardProcessResult(RedCardProcessResultCode.Retry, $"The following transactions from the driver file are currently queued for processing - {string.Join(", ", queuedTransactionIds.Select(t => t.ToString()))}");
            }

            // process the driver file
            return await this.ProcessInventoryDriver(
                inventoryDriverFilePath,
                tarFilePath,
                inventoryDriverFile,
                documentTransactions,
                _tarFileService.EnumerateAllFiles(tarFilePath)).ConfigureAwait(false);
        }

        private async Task<RedCardProcessResult> ProcessInventoryDriver(
            string inventoryDriverFileName,
            string tarFilePath,
            InventoryDriverFile inventoryDriverFile,
            IList<DocumentTransaction> documentTransactions,
            IEnumerable<TarFileRecord> tarFileContents)
        {
            var queueConfigurationSettings = new QueueConfigurationSettings();
            var coMessages = new Dictionary<Guid, InventoryDriverRecordMessage>();
            var recordFilesProcessed = new List<string>();
            var tarRecordFilesProcessed = new List<string>();
            var recordTypeProcessedCounts = new Dictionary<RedCardDocumentType, int>
            {
                {RedCardDocumentType.CO, 0},
                {RedCardDocumentType.EP, 0}
            };

            _logger.LogInformation("Begin Processing {TarRecordCount} Records for {FileName}", inventoryDriverFile.Records.Count, inventoryDriverFileName.Replace(Environment.NewLine, ""));

            _configuration.Bind("InventoryDriverRecordQueue", queueConfigurationSettings);

            var coGuidCounts = inventoryDriverFile.Records
                .Where(_ => _.DocumentType == RedCardDocumentType.CO)
                .GroupBy(_ => _.Guid)
                .ToDictionary(k => k.Key, v => v.Count());

            using (var messagePublisher = QuorumPublisherFactory.GetPublisher(queueConfigurationSettings, _loggerMessagePub))
            {
                foreach (var tarFileRecord in tarFileContents)
                {
                    tarRecordFilesProcessed.Add(tarFileRecord.FileName);

                    var record = inventoryDriverFile.Records
                        .FirstOrDefault(_ => _.DocFileName.Equals(tarFileRecord.FileName));

                    if (record == null)
                    {
                        _logger.LogInformation($"No record found in {inventoryDriverFileName.Replace(Environment.NewLine, "")} using {tarFileRecord.FileName.Replace(Environment.NewLine, "")} from {tarFilePath.Replace(Environment.NewLine, "")}");
                        continue;
                    }

                    if (!_pdfService.EndsWithEof(tarFileRecord.Data.Skip(tarFileRecord.Data.Length - 7).ToArray()))
                    {
                        _logger.LogError($"The following file is missing '%%EOF': {tarFileRecord.FileName}, skipped from processing");
                        continue;
                    }

                    InventoryDriverRecordMessage message;
                    switch (record.DocumentType)
                    {
                        case RedCardDocumentType.EP:
                            message = CreateEpMessage(inventoryDriverFileName, record, tarFileRecord);
                            await PublishMessage(messagePublisher, message).ConfigureAwait(false);
                            break;

                        case RedCardDocumentType.CO:
                            message = UpdateCoMessage(coMessages, inventoryDriverFileName, record, tarFileRecord);

                            if (message.DocumentFileNames.Count == coGuidCounts[record.Guid])
                            {
                                await ProcessMessage(message, documentTransactions.First(_ => _.TransactionID == message.TransactionId)).ConfigureAwait(false);

                                await PublishMessage(messagePublisher, message).ConfigureAwait(false);
                                coGuidCounts.Remove(message.Guid);
                                coMessages.Remove(message.Guid);
                            }
                            break;

                        default:
                            _logger.LogError($"Cannot process record type {record.DocumentType} for guid {record.Guid}");
                            continue;
                    }

                    recordTypeProcessedCounts[record.DocumentType] += 1;
                    recordFilesProcessed.Add(record.DocFileName);
                }
            }

            foreach (var (guid, record) in coMessages)
            {
                _logger.LogError("Could not find all documents for {DocumentType} record with guid {Guid}. Only found {TarGuidCount}/{DriverGuidCount} documents", record.DocumentType, guid, record.DocumentFileNames.Count, coGuidCounts[guid]);
            }

            _logger.LogInformation($"{recordTypeProcessedCounts[RedCardDocumentType.EP]:D3} {RedCardDocumentType.EP.ToString().Replace(Environment.NewLine, "")} records processed for {inventoryDriverFileName.Replace(Environment.NewLine, "")}");
            _logger.LogInformation($"{recordTypeProcessedCounts[RedCardDocumentType.CO]:D3} {RedCardDocumentType.CO.ToString().Replace(Environment.NewLine, "")} records processed for {inventoryDriverFileName.Replace(Environment.NewLine, "")}");
            _logger.LogInformation($"Completed processing INV.DRV file {inventoryDriverFileName.Replace(Environment.NewLine, "")} with {inventoryDriverFile.Footer.RecordCount} records");

            // validate missing files
            if (recordFilesProcessed.Count != inventoryDriverFile.Records.Count)
            {
                var missingFiles = recordFilesProcessed
                    .Except(tarRecordFilesProcessed)
                    .ToList();

                if (missingFiles.Any())
                {
                    _logger.LogAndThrowError($"The following files are missing from the tar {tarFilePath} : {string.Join(", ", missingFiles)}");
                }
            }

            return new RedCardProcessResult()
            {
                RecordsProcessed = recordFilesProcessed.Count,
            };
        }

        private InventoryDriverRecordMessage CreateEpMessage(string inventoryDriverFileName, InventoryDriverRecord inventoryDriverRecord, TarFileRecord tarFileRecord)
        {
            return new InventoryDriverRecordMessage
            {
                Guid = inventoryDriverRecord.Guid,
                DocumentFileNames = new List<string> { inventoryDriverRecord.DocFileName },
                DocumentType = inventoryDriverRecord.DocumentType,
                InventoryDriverFilePath = inventoryDriverFileName,
                PdfFiles = new List<KeyValuePair<string, string>> { KvpFromTarRecord(tarFileRecord) },
                PaymentId = inventoryDriverRecord.CPaymentID,
                TransactionId = inventoryDriverRecord.TransactionID,
                Tpa = inventoryDriverRecord.TPA,
            };
        }

        private InventoryDriverRecordMessage UpdateCoMessage(IDictionary<Guid, InventoryDriverRecordMessage> coMessages, string inventoryDriverFileName, InventoryDriverRecord inventoryDriverRecord, TarFileRecord tarFileRecord)
        {
            if (coMessages.TryGetValue(inventoryDriverRecord.Guid, out var value))
            {
                value.DocumentFileNames.Add(inventoryDriverRecord.DocFileName);
                value.PdfFiles = value.PdfFiles.Append(KvpFromTarRecord(tarFileRecord));
            }
            else
            {
                coMessages.Add(inventoryDriverRecord.Guid, new InventoryDriverRecordMessage
                {
                    Guid = inventoryDriverRecord.Guid,
                    DocumentFileNames = new List<string> { inventoryDriverRecord.DocFileName },
                    DocumentType = inventoryDriverRecord.DocumentType,
                    InventoryDriverFilePath = inventoryDriverFileName,
                    PaymentId = inventoryDriverRecord.CPaymentID,
                    PdfFiles = new List<KeyValuePair<string, string>> { KvpFromTarRecord(tarFileRecord) },
                    TransactionId = inventoryDriverRecord.TransactionID,
                    Tpa = inventoryDriverRecord.TPA,
                });
            }

            return coMessages[inventoryDriverRecord.Guid];
        }

        private static KeyValuePair<string, string> KvpFromTarRecord(TarFileRecord tarFileRecord)
        {
            return new KeyValuePair<string, string>(tarFileRecord.FileName, Convert.ToBase64String(tarFileRecord.Data));
        }

        private async Task PublishMessage(IMessagePublisher messagePublisher, InventoryDriverRecordMessage message)
        {
            await _redCardRepository.UpdateTransactionStatus(message.TransactionId, RedCardTransactionStatusCode.StapleDocumentsMessageQueued).ConfigureAwait(false);

            try
            {
                messagePublisher.Publish(JsonConvert.SerializeObject(message));
            }
            catch (Exception)
            {
                await _redCardRepository.ClearTransactionStatus(message.TransactionId, RedCardTransactionStatusCode.StapleDocumentsMessageQueued).ConfigureAwait(false);
                throw;
            }

            _logger.LogDebug($"Queued inventoryDriverRecord={message}");
        }

        private async Task ProcessMessage(InventoryDriverRecordMessage message, DocumentTransaction documentTransaction)
        {
            using var l = _logger.BeginScope(new Dictionary<string, object>
            {
                { "Guid", message.Guid },
                { "TransactionId", message.TransactionId },
            });

            // CO messages normally have more than one file to add
            // OPACH PayType messages only have the EOB file and need to add
            // the cover page manually
            if (message.DocumentFileNames.Count == 1 &&
                documentTransaction.PaymentTypeCode == PayType.OPACH)
            {
                var coverPage = await GetCoverPage(message.TransactionId).ConfigureAwait(false);

                // add cover page
                if (coverPage.HasValue)
                {
                    message.DocumentFileNames.Add(coverPage.Value.Key);
                    message.PdfFiles = message.PdfFiles.Append(coverPage.Value);

                    _logger.LogInformation("Added cover page {FileName} to documents", coverPage.Value.Key, message.TransactionId);
                }
            }
        }

        private async Task<KeyValuePair<string, string>?> GetCoverPage(long transactionId)
        {
            var fileName = VPaygeDocumentType.Has_Cover.GetAttribute<DocFileExtensionAttribute>().FileExtension;
            var archives = await _storageManager.GetArchivesForTransactionId(transactionId).ConfigureAwait(false);
            var archiveDetail = archives
                .Select(_ => _.GetArchiveDetailByName(fileName))
                .FirstOrDefault(_ => _ != null);

            if (archiveDetail == null)
            {
                _logger.LogError("'{FileName}' flag missing from {TransactionId} archives, cannot find flag for cover page", fileName, transactionId);
                return null;
            }

            using var flagDetailStream = await _storageManager.GetArchiveDetailStream(archiveDetail.ArchiveDetailId).ConfigureAwait(false);
            using var flagStream = flagDetailStream.Open();
            using var flagStreamReader = new StreamReader(flagStream);
            var coverFileName = flagStreamReader.ReadToEnd();

            archiveDetail = archives
                .Select(_ => _.GetArchiveDetailByName(coverFileName))
                .FirstOrDefault(_ => _ != null);

            if (archiveDetail == null)
            {
                _logger.LogError("'{CoverFileName}' file missing from {TransactionId} archives, cannot find cover page", fileName, transactionId);
                return null;
            }

            using var coverDetailStream = await _storageManager.GetArchiveDetailStream(archiveDetail.ArchiveDetailId).ConfigureAwait(false);
            using var coverStream = coverDetailStream.Open();

            var ms = new MemoryStream();
            await coverStream.CopyToAsync(ms).ConfigureAwait(false);

            return new KeyValuePair<string, string>(coverFileName, Convert.ToBase64String(ms.ToArray()));
        }

        #region [-- implements IDriverFileService --]

        bool IDriverFileService.IsLocked(string driverFile) => _driverFileService.IsLocked(driverFile);

        bool IDriverFileService.GetLock(string driverFile) => _driverFileService.GetLock(driverFile);

        bool IDriverFileService.ReleaseLock(string driverFile) => _driverFileService.ReleaseLock(driverFile);

        bool IDriverFileService.WriteLogFile(string driverFile, string content) => _driverFileService.WriteLogFile(driverFile, content);

        bool IDriverFileService.DeleteLogFile(string driverFile) => _driverFileService.DeleteLogFile(driverFile);

        string IDriverFileService.GetLockFileName(string driverFile) => _driverFileService.GetLockFileName(driverFile);

        string IDriverFileService.GetLogFileName(string driverFile) => _driverFileService.GetLogFileName(driverFile);

        #endregion
    }
}
