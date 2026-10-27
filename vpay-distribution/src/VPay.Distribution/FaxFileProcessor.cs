using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using System.Threading.Tasks;

using VPay.Distribution.Common;
using VPay.DocSys.Common;
using VPay.DocSys.Data;

namespace VPay.Distribution
{
    public class FaxFileProcessor : DistributionProcessor, IFaxFileProcessor
    {
        public FaxFileProcessor(IConfiguration configuration, IFileSystem fileSystem, IDocSysHttpClient docSysClient, IDistributionRepository distributionRepository, ApiSettings apiSettings, ILogger<FaxFileProcessor> logger)
            : base(configuration, fileSystem, docSysClient, distributionRepository, apiSettings, logger)
        {
        }

        public override async Task<DistributionProcessResult> Process(DocDistMsg message, bool createControlFile = true)
        {
            // NOTE: this method is overridden in RedCardFaxFileProcessor for red card fax distribution

            using (_logger.BeginScope(new Dictionary<string, object>()
            {
                {"Processor", this.GetType().Name },
                {"STSFile", message.StatusFilePath},
                {"TPA", message.TPA },
                {"ClientCode", message.TPA },
            }))
            {
                if (!(await _distributionRepository.TryUpdateDistributionStatus(message.PaymentStatusFileId, DistributionMethods.Fax)))
                {
                    _logger.LogInformation($"Faxes already distributed for file {message.PaymentStatusFileId}.");
                    return new DistributionProcessResult("FAX");
                }
                try
                {
                    await _distributionRepository.UpdateFileStatus(message.PaymentStatusFileId, FileStatusCode.FaxDistBegin);
                    var msgResult = await this.ProcessMessage(message, createControlFile);
                    await _distributionRepository.UpdateFileStatus(message.PaymentStatusFileId, FileStatusCode.FaxDistComplete);
                    return msgResult;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    await _distributionRepository.ClearDistributionStatus(message.PaymentStatusFileId, DistributionMethods.Fax);
                    await _distributionRepository.UpdateFileStatus(message.PaymentStatusFileId, FileStatusCode.Error);
                    throw;
                }
            }
        }

        public string CreateFaxHeader(string stsFilePath)
        {
            var timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            var stsfileName = _fileSystem.Path.GetFileName(stsFilePath);

            return $"00|{timestamp}|fax release file|{stsfileName}";
        }

        public async Task<string> WriteFaxFile(string stsFilePath, string faxLines)
        {
            var faxFile = GetFaxFileName(stsFilePath);

            var faxFileDirectory = _fileSystem.Path.GetDirectoryName(faxFile);

            _fileSystem.Directory.CreateDirectory(faxFileDirectory);

            _logger.LogInformation($"Writing fax file: {faxFile}");

            using (var stream = _fileSystem.File.CreateText(faxFile))
            {
                var faxFilePath = ((FileStream)(stream.BaseStream)).Name;
                await stream.WriteAsync(faxLines);
                stream.Flush();
                stream.Close();

                return faxFilePath;
            }
        }

        public string CreateFaxLine(FaxFileRecord record)
        {
            string value = $"05|{record.TransactionId}|{record.PayType}|{record.DistType}|{record.Status}|{record.FilePath}";
            if (record.NewGuid.HasValue)
            {
                value = $"{value}|{record.NewGuid.Value}";
            }
            return value;
        }

        public string CreateFaxFooter(int count)
        {
            var timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            return $"99|{timestamp}|{count}|0";
        }

        protected virtual async Task<DistributionProcessResult> ProcessMessage(DocDistMsg message, bool createControlFile = true)
        {
            _logger.LogInformation($"Initiating Fax distribution for {message.StatusFilePath}");

            var tpaConfig = await _docSysClient.GetDocSysConfig(message.TPA);

            var transferPath = ConstructBasePath(message);
            var baseFaxFilePath = GetConfigValue<string>("faxbase", tpaConfig);
            var baseCtrlFilePath = GetConfigValue<string>("ControlPath", tpaConfig);
            var fullFaxFilePath = _fileSystem.Path.Combine(baseFaxFilePath ?? "", transferPath);

            _fileSystem.Directory.CreateDirectory(fullFaxFilePath);

            _logger.LogInformation($"Extracting documents to fax to: {fullFaxFilePath}");

            var faxRecords = new List<FaxFileRecord>();

            foreach (var record in message.Transactions)
            {
                var faxRecord = await CreateFaxRecord(record, fullFaxFilePath);
                if (faxRecord != null)
                    faxRecords.Add(faxRecord);
            }

            if (faxRecords.Count == 0)
            {
                _logger.LogInformation($"No transactions to be faxed for payment status file - PaymentStatusFileID: {message.PaymentStatusFileId}");
                return new DistributionProcessResult("FAX");
            }
            else
            {
                _logger.LogInformation($"Created {faxRecords.Count} fax records.");

                var faxFileContent = new StringBuilder();
                faxFileContent.AppendLine(CreateFaxHeader(message.StatusFilePath));
                foreach (var record in faxRecords)
                    faxFileContent.AppendLine(CreateFaxLine(record));
                faxFileContent.AppendLine(CreateFaxFooter(faxRecords.Count));

                var result = await WriteFaxFile(message.StatusFilePath, faxFileContent.ToString());

                var controlFile = createControlFile
                    ? CreateControlFile(result, baseCtrlFilePath)
                    : null;

                // TODO: once this warning has not been logged and we do not need the legacy fax processing, this should be deleted
                if (controlFile != null)
                {
                    _logger.LogInformation(
                        "A control file {ControlFileName} was created for {TPA}, this is only for legacy fax distribution!",
                        controlFile, message.TPA);
                }

                return new DistributionProcessResult("FAX")
                {
                    Count = faxRecords.Count,
                    FileName = result,
                    ControlFileName = controlFile
                };
            }
        }

#pragma warning disable 1998
        // Async is needed for the RedCardFaxFileProcessor which overrides this method
        protected virtual async Task<FaxFileRecord> CreateFaxRecord(DistTxnData transactionRecord, string archiveFilePath)
#pragma warning restore 1998
        {
            using (_logger.BeginScope(new Dictionary<string, object>()
            {
                { "TransactionId", transactionRecord.TxnId },
                { "PayType", transactionRecord.PayType },
                { "ArchiveFilePath", archiveFilePath },
                { "Operation", nameof(CreateFaxRecord) },
            }))
            {
                try
                {
                    return new FaxFileRecord
                    {
                        TransactionId = transactionRecord.TxnId.ToString(),
                        PayType = transactionRecord.PayType,
                        DistType = transactionRecord.DistType.ToString(),
                        Status = "*",
                        FilePath = string.Empty
                    };
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, $"Unable to retrieve archive detail PDF for transaction {transactionRecord.TxnId}.");
                    return null;
                }
            }
        }

        protected virtual string GetFaxFileName(string stsFilePath)
        {
            var faxFileName = $"{_fileSystem.Path.GetFileNameWithoutExtension(stsFilePath)}.fax";
            var faxFilePath = _fileSystem.Path.GetDirectoryName(stsFilePath);

            return _fileSystem.Path.Combine(faxFilePath, faxFileName);
        }
    }
}
