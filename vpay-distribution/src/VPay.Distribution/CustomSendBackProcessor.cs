using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using VPay.Distribution.Common;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Storage;

namespace VPay.Distribution
{
    public class CustomSendBackProcessor : DistributionProcessor, ICustomSendBackProcessor
    {
        private readonly IPdfTools _pdfTools;
        private readonly IStorageApiClient _storageApiClient;
        private readonly IRedCardRepository _redCardRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomSendBackProcessor"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="docSysClient">The document system client.</param>
        /// <param name="storageApiClient">The storage API client.</param>
        /// <param name="sqlOperations">The SQL operations.</param>
        /// <param name="apiSettings">The API settings.</param>
        /// <param name="pdfTools">The PDF tools.</param>
        /// <param name="logger">The logger.</param>
        public CustomSendBackProcessor(
            IConfiguration configuration,
            IFileSystem fileSystem,
            IDocSysHttpClient docSysClient,
            IStorageApiClient storageApiClient,
            IDistributionRepository distributionRepository,
            IRedCardRepository redCardRepository,
            ApiSettings apiSettings,
            IPdfTools pdfTools,
            ILogger<CustomSendBackProcessor> logger)
            : base(configuration, fileSystem, docSysClient, distributionRepository, apiSettings, logger)
        {
            ArgumentHelpers.ThrowIf.IsNull(pdfTools, nameof(pdfTools));
            ArgumentHelpers.ThrowIf.IsNull(storageApiClient, nameof(storageApiClient));
            ArgumentHelpers.ThrowIf.IsNull(redCardRepository, nameof(redCardRepository));

            _pdfTools = pdfTools;
            _storageApiClient = storageApiClient;
            _redCardRepository = redCardRepository;
        }

        /// <inheritdoc/>
        public override Task<DistributionProcessResult> Process(DocDistMsg message, bool createControlFile = true)
        {
            return this.Process(message, default);
        }

        /// <inheritdoc/>
        public async Task<DistributionProcessResult> Process(DocDistMsg distributionMessage, CancellationToken cancellationToken)
        {
            ArgumentHelpers.ThrowIf.IsNull(distributionMessage, nameof(distributionMessage));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(distributionMessage.Transactions, $"{nameof(distributionMessage)}.{nameof(distributionMessage.Transactions)}");
            ArgumentHelpers.ThrowIf.IsNullOrWhiteSpace(distributionMessage.StatusFilePath, $"{nameof(distributionMessage)}.{nameof(distributionMessage.StatusFilePath)}");

            using (_logger.BeginScope(new Dictionary<string, object>()
            {
                { "Processor", this.GetType().Name },
                { "StatusFile", distributionMessage.StatusFilePath},
                { "StatusFileId", distributionMessage.PaymentStatusFileId},
                { "Origin", distributionMessage.Origin },
                { "OriginCode", distributionMessage.Origin },
                { "TPA", distributionMessage.TPA },
                { "ClientCode", distributionMessage.TPA },
                { "Transactions", distributionMessage.Transactions.Count },
            }))
            {
                try
                {
                    if (distributionMessage.Transactions.Any(t => t.DistType != 'S'))
                    {
                        throw new Exception($"Custom Send Back distribution message contains one or more transactions with an invalid {nameof(DistTxnData.DistType)}.");
                    }
                    else if (!distributionMessage.Transactions.Any(t => t.DistType == 'S'))
                    {
                        _logger.LogInformation($"Distribution message contains no payments that require Custom Send Back distribution.");
                        return new DistributionProcessResult("CSB");
                    }
                    else if(!(await _distributionRepository.TryUpdateDistributionStatus(distributionMessage.PaymentStatusFileId, DistributionMethod.CustomSendBack)))
                    {
                        _logger.LogInformation($"Custom Send Back distribution already completed for payment status file.");
                        return new DistributionProcessResult("CSB");
                    }

                    await _distributionRepository.UpdateFileStatus(distributionMessage.PaymentStatusFileId, FileStatusCode.CustomSendBackDistBegin);

                    var distributionPacket = await this.GenerateDistributionPacket(distributionMessage, cancellationToken);

                    var result = await this.WriteDistributionPacket(distributionMessage, distributionPacket);

                    await _distributionRepository.UpdateFileStatus(distributionMessage.PaymentStatusFileId, FileStatusCode.CustomSendBackDistComplete);

                    // for red card transactions, update RedCardTransactionStatus to 255-Transaction Complete
                    if (distributionMessage.IsRedCard)
                    {
                        foreach (var txnId in distributionMessage.Transactions.Where(t => t.DistType == 'S').Select(t => t.TxnId))
                        {
                            // since the send back distribution is complete at this point, we don't want the whole message to fail if a status update fails
                            try
                            {
                                await _redCardRepository.UpdateTransactionStatus(txnId, RedCardTransactionStatusCode.TransactionComplete, cancellationToken);
                            }
                            catch (Exception exception)
                            {
                                _logger.LogError(exception, $"An error occurred while trying to update RedCardTransactionStatus to {nameof(RedCardTransactionStatusCode.TransactionComplete)} for transaction: {txnId}");
                            }
                        }
                    }

                    return result;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, $"Distribution for Custom Send Back check failed for file {distributionMessage.PaymentStatusFileId} - {exception.Message}");
                    await _distributionRepository.ClearDistributionStatus(distributionMessage.PaymentStatusFileId, DistributionMethod.CustomSendBack);
                    await _distributionRepository.UpdateFileStatus(distributionMessage.PaymentStatusFileId, FileStatusCode.Error);
                    throw;
                }
            }
        }

        private async Task<byte[]> GenerateDistributionPacket(DocDistMsg distributionMessage, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"CustomSendBack Distribution - GenerateDistributionPacket.");

            var pdfBuffers = new List<byte[]>();

            foreach (var txn in distributionMessage.Transactions.Where(t => t.DistType == 'S'))
            {
                var checkPdf = await _storageApiClient.GetCustomSendBackCheck(txn.TxnId, cancellationToken);

                if (checkPdf == null)
                {
                    throw new Exception($"SendBack check pdf not found for transaction {txn.TxnId}.");
                }

                pdfBuffers.Add(checkPdf);
            }

            var mergedPdf = await _pdfTools.MergeAsync(pdfBuffers.ToArray());

            return mergedPdf;
        }

        private Task<DistributionProcessResult> WriteDistributionPacket(DocDistMsg distributionMessage, byte[] customSendBackPdf)
        {
            _logger.LogInformation($"CustomSendBack Distribution - WriteDistributionPacket.");

            ArgumentHelpers.ThrowIf.IsNull(distributionMessage, nameof(distributionMessage));
            ArgumentHelpers.ThrowIf.IsNull(distributionMessage.Transactions, $"{nameof(distributionMessage)}.{nameof(distributionMessage.Transactions)}");
            ArgumentHelpers.ThrowIf.IsNullOrWhiteSpace(distributionMessage.StatusFilePath, $"{nameof(distributionMessage)}.{nameof(distributionMessage.StatusFilePath)}");
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(customSendBackPdf, nameof(customSendBackPdf));

            if (distributionMessage.Transactions.Count == 0)
            {
                throw new ArgumentException("Message contains zero transactions and cannot be processed.", nameof(distributionMessage));
            }

            var fileName = $"{_fileSystem.Path.GetFileNameWithoutExtension(distributionMessage.StatusFilePath)}.{distributionMessage.Transactions.First().TxnId}.{Guid.NewGuid()}_master.pdf";

            var fileDropPath = this.GetCustomSendBackDropFolderPath();

            var fullFilePath = _fileSystem.Path.Combine(fileDropPath, fileName);

            _fileSystem.File.WriteAllBytes(fullFilePath, customSendBackPdf);

            return Task.FromResult(new DistributionProcessResult("CSB")
            {
                Count = distributionMessage.Transactions.Count,
                FileName = fullFilePath,
            });
        }

        private string GetCustomSendBackDropFolderPath()
        {
            var basePath = _configuration.GetSection("AppSettings").GetValue<string>("CustomSendBackPath");

            if (string.IsNullOrWhiteSpace(basePath))
            {
                throw new Exception("Value missing or empty for AppSettings.CustomSendBackPath.");
            }
            else if (!_fileSystem.Directory.Exists(basePath))
            {
                throw new Exception($"The path specified in AppSettings.CustomSendBackPath does not exist [{basePath}].");
            }

            var date = DateTime.Now;

            var day = date.Day.ToString("D2");
            var month = date.Month.ToString("D2");
            var year = date.Year.ToString("D4");

            var dropFolderPath = _fileSystem.Path.Combine(basePath, year, month, day);

            _fileSystem.Directory.CreateDirectory(dropFolderPath);

            return dropFolderPath;
        }
    }
}
