using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Parsing.Common
{
    public class OutboundRedCardDataDriverFileProcessor : IOutboundRedCardDataDriverFileProcessor
    {
        private readonly IOutboundRedCardDataDriverStatusWriter _outboundRedCardDataDriverStatusWriter;
        private readonly IDistributionApiClient _distributionClientApi;
        private readonly RedCardDataScpSettings _redCardDataScpSettings;
        private readonly IArchiveService _archiveService;
        private readonly ITarFileService _tarFileService;
        private readonly IFileSystem _fileSystem;
        private readonly IRedCardRepository _redCardRepository;
        private readonly ILogger<OutboundRedCardDataDriverFileProcessor> _logger;

        public OutboundRedCardDataDriverFileProcessor(
            IOutboundRedCardDataDriverStatusWriter outboundRedCardDataDriverStatusWriter,
            IDistributionApiClient distributionClientApi,
            RedCardDataScpSettings redCardDataScpSettings,
            IArchiveService archiveService,
            ITarFileService tarService,
            IFileSystem fileSystem,
            IRedCardRepository redCardRepository,
            ILogger<OutboundRedCardDataDriverFileProcessor> logger)
        {
            _outboundRedCardDataDriverStatusWriter = outboundRedCardDataDriverStatusWriter;
            _distributionClientApi = distributionClientApi;
            _redCardDataScpSettings = redCardDataScpSettings;
            _archiveService = archiveService;
            _tarFileService = tarService;
            _fileSystem = fileSystem;
            _redCardRepository = redCardRepository;
            _logger = logger;
        }

        public async Task<RedCardProcessResult> Process(OutboundRedCardDataDriverMessage message)
        {
            using (_logger.BeginScope(new Dictionary<string, object>()
            {
                {"OutboundRedCardDataDriverFile", message.DataDriverFileName}
            }))
            {
                var statusHistory = await _redCardRepository.GetTransactionStatusHistoryForStatusFileId(message.PaymentStatusFileId);

                if (!statusHistory.AllTransactionsHaveStatus(RedCardTransactionStatusCode.DocumentsGenerated))
                {
                    return new RedCardProcessResult(RedCardProcessResultCode.Retry, $"Documents have not been generated for one or more transactions for Payment Status File Id: {message.PaymentStatusFileId}");
                }

                _logger.LogInformation($"Processing file: {message.DataDriverFileName}");

                var tarFilePath = _fileSystem.Path.ChangeExtension(message.DataDriverFileName, "tar");

                try
                {
                    _logger.LogInformation($"Initiating .STATUS creation for {message.DataDriverFileName}");

                    var archiveDetails = await ValidateRecords(message);

                    var foundGuids = UpdateArchiveFile(tarFilePath, message.DriverFile.Records, archiveDetails);

                    UpdateRecordStatus(message.DriverFile.Records, foundGuids);

                    var statusFilePath = WriteStatusFile(message.DataDriverFileName, message.DriverFile);

                    var files = await TransferFiles(statusFilePath, tarFilePath);

                    await UpdateTransactionStatus(message.DriverFile.Records);

                    _logger.LogInformation($"Completed processing outbound_rc_data.drv file {message.DataDriverFileName} with {files.Count} records");

                    return new RedCardProcessResult()
                    {
                        RecordsProcessed = message.DriverFile.Records.Count,
                        FileNames = files.Select(_=>_.FilePath).ToList(),
                        ControlFileNames = files.Select(_ => _.ControlFileName).ToList(),
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    throw;
                }
            }
        }

        private async Task<IEnumerable<IArchiveDetail>> ValidateRecords(OutboundRedCardDataDriverMessage message)
        {
            var results = new List<IArchiveDetail>();

            _logger.LogInformation($"Start validating all records for {message.DriverFile}");

            foreach (var record in message.DriverFile.Records)
            {
                IArchiveDetail archiveDetail = null;

                try
                {
                    archiveDetail = await _archiveService.GetArchiveDetailForTransactionIDAndFileName(
                        record.TransactionID,
                        record.CoverFileName);
                }
                catch (ArgumentException)
                {
                    _logger.LogWarning($"No archive found for record id = {record.Guid}");
                    record.Status = OutboundRedCardDataDriverRecordStatus.DataNotFound;
                }

                if (archiveDetail == null)
                {
                    _logger.LogWarning($"No archive details found for record id = {record.Guid} and cover name = {record.CoverFileName}");
                    record.Status = OutboundRedCardDataDriverRecordStatus.CoverSheetMissing;
                }
                else
                {
                    _logger.LogInformation($"Found {archiveDetail.FileName} from {record.Guid}");
                    results.Add(archiveDetail);
                }
            }

            _logger.LogInformation($"Finish validating all records for {message.DriverFile}");

            return results;
        }

        private List<Guid> UpdateArchiveFile(string tarFilePath, IReadOnlyCollection<OutboundRedCardDataDriverRecord> records, IEnumerable<IArchiveDetail> archiveDetails)
        {
            var tarRecords = archiveDetails
                .Select(_ =>
                {
                    // this should never fail
                    var record = records
                        .Single(__ => __.CoverFileName.Equals(_.FileName, StringComparison.InvariantCultureIgnoreCase));

                    return new TarFileRecord
                    {
                        Guid = record.Guid,
                        FileName = _.FileName,
                        Data = _.Data,
                        TarRecordName = record.MemberFileName
                    };
                })
                .ToList();

            return _tarFileService.CreateTarFile(tarFilePath, tarRecords);
        }

        private void UpdateRecordStatus(IEnumerable<OutboundRedCardDataDriverRecord> records, ICollection<Guid> updatedGuids)
        {
            foreach (var record in records)
            {
                record.Status = updatedGuids.Contains(record.Guid) &&
                                record.Status == OutboundRedCardDataDriverRecordStatus.Unknown
                    ? OutboundRedCardDataDriverRecordStatus.Validated
                    : OutboundRedCardDataDriverRecordStatus.ArchiveUpdateFailed;
            }
        }

        private async Task UpdateTransactionStatus(IEnumerable<OutboundRedCardDataDriverRecord> records)
        {
            foreach (var record in records)
            {
                if(record.Status == OutboundRedCardDataDriverRecordStatus.Validated)
                {
                    await _redCardRepository.UpdateTransactionStatus(record.TransactionID, RedCardTransactionStatusCode.DocumentsSentToRedCard);
                }
            }
        }

        private string WriteStatusFile(string dataDriverFileName, OutboundRedCardDataDriverFile driverFile)
        {
            var statusFileName = _fileSystem.Path.ChangeExtension(dataDriverFileName, "status");

            _logger.LogInformation($"Creating {statusFileName}");

            using (var stream = _fileSystem.File.CreateText(statusFileName))
            {
                _logger.LogInformation($"Writing to {statusFileName}");
                _outboundRedCardDataDriverStatusWriter.Write(stream, driverFile);
            }

            return statusFileName;
        }

        private async Task<IList<ServerFile>> TransferFiles(string statusFilePath, string tarFilePath)
        {
            var statusFileName = _fileSystem.Path.GetFileName(statusFilePath);
            var tarFileName = _fileSystem.Path.GetFileName(tarFilePath);

            _logger.LogInformation($"Copying files {statusFileName} and {tarFileName} to {_redCardDataScpSettings.Host}");

            if (_redCardDataScpSettings.TransferFiles)
            {
                var files = await _distributionClientApi.FtpFiles(_redCardDataScpSettings, statusFilePath, tarFilePath);

                return files;
            }

            return new List<ServerFile>();
        }
    }
}
