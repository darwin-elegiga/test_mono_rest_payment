using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;

using VPay.Distribution.Common;
using VPay.DocSys.Common;
using VPay.DocSys.Data;

namespace VPay.Distribution
{
    public class RedCardFaxFileProcessor : FaxFileProcessor, IRedCardFaxFileProcessor
    {
        private IRedCardRepository _redCardRepository;

        public RedCardFaxFileProcessor(
            IConfiguration configuration,
            IFileSystem fileSystem,
            IDocSysHttpClient docSysClient,
            IDistributionRepository distributionRepository,
            IRedCardRepository redCardRepository,
            ApiSettings apiSettings,
            ILogger<FaxFileProcessor> logger) : base(configuration, fileSystem, docSysClient, distributionRepository, apiSettings, logger)
        {
            _redCardRepository = redCardRepository;
        }

        public override async Task<DistributionProcessResult> Process(DocDistMsg message, bool createControlFile = true)
        {
            // NOTE:    this method is overridden for red card because the non-red card fax processor only allows
            //          distribution once per .sts file and distribution for red card transactions in a given .sts file
            //          may be distributed at different times; so, there's no check for this in this override

            using (_logger.BeginScope(new Dictionary<string, object>()
            {
                {"Processor", this.GetType().Name },
                {"STSFile", message.StatusFilePath},
                {"TPA", message.TPA },
                {"ClientCode", message.TPA },
            }))
            {
                try
                {
                    await _distributionRepository.UpdateFileStatus(message.PaymentStatusFileId, FileStatusCode.FaxDistBegin);
                    var result = await this.ProcessMessage(message, createControlFile);
                    await _distributionRepository.UpdateFileStatus(message.PaymentStatusFileId, FileStatusCode.FaxDistComplete);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    await _distributionRepository.UpdateFileStatus(message.PaymentStatusFileId, FileStatusCode.Error);
                    throw;
                }
            }
        }

        protected override async Task<FaxFileRecord> CreateFaxRecord(DistTxnData transactionRecord, string archiveFilePath)
        {
            if ((await _redCardRepository.GetTransactionStatus(transactionRecord.TxnId)) != RedCardTransactionStatusCode.ReadyForDistribution)
            {
                if (await _redCardRepository.GetTransactionHasStatus(transactionRecord.TxnId, RedCardTransactionStatusCode.SentToDistribution))
                {
                    _logger.LogInformation($"Transaction has already been distributed - TransactionID: {transactionRecord.TxnId}");
                }
                else if (await _redCardRepository.GetTransactionHasStatus(transactionRecord.TxnId, RedCardTransactionStatusCode.TransactionComplete))
                {
                    _logger.LogError($"Transaction cannot be distributed - TransactionID: {transactionRecord.TxnId}");
                }
                else
                {
                    _logger.LogError($"Transaction is not Ready For Distribution - TransactionID: {transactionRecord.TxnId}");
                }

                return null;
            }
            else if (!(await _redCardRepository.TryUpdateTransactionDistributionStatus(transactionRecord.TxnId)))
            {
                _logger.LogInformation($"Transaction has already been distributed - TransactionID: {transactionRecord.TxnId}");
                return null;
            }

            try
            {
                var faxRecord = await base.CreateFaxRecord(transactionRecord, archiveFilePath);

                if (faxRecord == null)
                {
                    await _redCardRepository.ClearTransactionDistributionStatus(transactionRecord.TxnId);
                }
                else
                {
                    await _redCardRepository.UpdateTransactionStatus(transactionRecord.TxnId, RedCardTransactionStatusCode.TransactionComplete);
                }

                return faxRecord;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, exception.Message);
                await _redCardRepository.ClearTransactionDistributionStatus(transactionRecord.TxnId);
                return null;
            }
        }

        protected override string GetFaxFileName(string stsFilePath)
        {
            var faxFilePath = _fileSystem.Path.GetDirectoryName(stsFilePath);

            var faxFileNameNoExtension = _fileSystem.Path.GetFileNameWithoutExtension(stsFilePath);

            var fullFaxFileNameNoExtension = _fileSystem.Path.Combine(faxFilePath, faxFileNameNoExtension);

            // check for the existence of .fax and for .fax.proc or .fax.active which used by the Redo Fax Failure Process
            var dotFax = $"{fullFaxFileNameNoExtension}.fax";
            var dotFaxDotProc = $"{fullFaxFileNameNoExtension}.fax.proc";
            var dotFaxDotActive = $"{fullFaxFileNameNoExtension}.fax.active";

            // if any already exist, go to .xxx.fax incremental naming
            if (_fileSystem.File.Exists(dotFax) || _fileSystem.File.Exists(dotFaxDotProc) || _fileSystem.File.Exists(dotFaxDotActive))
            {
                var index = 0;

                dotFax = $"{fullFaxFileNameNoExtension}.{++index:D3}.fax";

                while (_fileSystem.File.Exists(dotFax))
                {
                    dotFax = $"{fullFaxFileNameNoExtension}.{++index:D3}.fax";
                }
            }

            return dotFax;
        }
    }
}
