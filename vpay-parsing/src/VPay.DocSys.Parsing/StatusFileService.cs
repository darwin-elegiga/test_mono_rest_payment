using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Extensions;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.DocSys.Parsing.Common.Rabbitmq;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto;
using VPay.Parsing.Common;
using VPay.Queuing;

namespace VPay.DocSys.Parsing
{
    public class StatusFileService : IStatusFileService
    {
        private readonly IStatusFilePathHelper _statusFilePathHelper;
        private readonly IConfiguration _configuration;
        private readonly IDocSysConfigService _docSysConfig;
        private readonly ITarService _tarService;
        private readonly IRedCardRepository _redCardRepository;
        private readonly IAuxiliaryFileWriter _auxiliaryFileWriter;
        private readonly IDocumentTransactionMessageListBuilder _documentTransactionMessageListBuilder;
        private readonly IDistributionMessageBuilder _distributionMessageBuilder;
        private readonly ILogger _logger;
        private readonly ILogger<IMessagePublisher> _loggerMessagePub;
        private readonly IStatusFileTracker _tracker;
        private readonly IEdi835TarService _edi835TarService;
        private readonly IPlatformConfigurationApi _platformConfigurationApi;
        private readonly IPaymentEngineConfirmationFileService _paymentEngineConfirmationFileService;
        private readonly HttpRequestPolicyHolder _httpRequestPolicies;
        private readonly bool _enableEdi835Parsing = false;
        private readonly IRabbitMqPublisherFactory _rabbitMqPublisherFactory;

        // default amount of time to wait before warning that distribution is not complete
        private const int DefaultDistributionWarningDelayInHours = 1;

        // the red card process takes considerably longer to complete, so use a different value for red card distribution
        private const int RedCardDistributionWarningDelayInHours = 72;

        public StatusFileService(
            IStatusFilePathHelper statusFilePathHelper,
            IConfiguration configuration,
            IDocSysConfigService docSysConfig,
            ITarService tarService,
            IRedCardRepository redCardRepository,
            IAuxiliaryFileWriter auxiliaryFileWriter,
            IDocumentTransactionMessageListBuilder documentTransactionMessageListBuilder,
            IDistributionMessageBuilder distributionMessageBuilder,
            IStatusFileTracker statusFileTracker,
            IEdi835TarService edi835TarService,
            IPlatformConfigurationApi platformConfigurationApi,
            IPaymentEngineConfirmationFileService paymentEngineConfirmationFileService,
            ILoggerFactory loggerFactory,
            IRabbitMqPublisherFactory rabbitMqPublisherFactory)
        {
            ArgumentHelpers.ThrowIf.IsNull(statusFilePathHelper, nameof(statusFilePathHelper));
            ArgumentHelpers.ThrowIf.IsNull(configuration, nameof(configuration));
            ArgumentHelpers.ThrowIf.IsNull(docSysConfig, nameof(docSysConfig));
            ArgumentHelpers.ThrowIf.IsNull(tarService, nameof(tarService));
            ArgumentHelpers.ThrowIf.IsNull(redCardRepository, nameof(redCardRepository));
            ArgumentHelpers.ThrowIf.IsNull(auxiliaryFileWriter, nameof(auxiliaryFileWriter));
            ArgumentHelpers.ThrowIf.IsNull(documentTransactionMessageListBuilder, nameof(documentTransactionMessageListBuilder));
            ArgumentHelpers.ThrowIf.IsNull(distributionMessageBuilder, nameof(distributionMessageBuilder));
            ArgumentHelpers.ThrowIf.IsNull(statusFileTracker, nameof(statusFileTracker));
            ArgumentHelpers.ThrowIf.IsNull(edi835TarService, nameof(edi835TarService));
            ArgumentHelpers.ThrowIf.IsNull(platformConfigurationApi, nameof(platformConfigurationApi));
            ArgumentHelpers.ThrowIf.IsNull(paymentEngineConfirmationFileService, nameof(paymentEngineConfirmationFileService));
            ArgumentHelpers.ThrowIf.IsNull(loggerFactory, nameof(loggerFactory));
            ArgumentHelpers.ThrowIf.IsNull(rabbitMqPublisherFactory, nameof(rabbitMqPublisherFactory));

            _statusFilePathHelper = statusFilePathHelper;
            _configuration = configuration;
            _docSysConfig = docSysConfig;
            _tarService = tarService;
            _redCardRepository = redCardRepository;
            _auxiliaryFileWriter = auxiliaryFileWriter;
            _documentTransactionMessageListBuilder = documentTransactionMessageListBuilder;
            _distributionMessageBuilder = distributionMessageBuilder;
            _tracker = statusFileTracker;
            _edi835TarService = edi835TarService;
            _platformConfigurationApi = platformConfigurationApi;
            _paymentEngineConfirmationFileService = paymentEngineConfirmationFileService;
            _logger = loggerFactory.CreateLogger<StatusFileService>();
            _loggerMessagePub = loggerFactory.CreateLogger<IMessagePublisher>();

            _enableEdi835Parsing = _edi835TarService.EnableEdi835Parsing;

            var apiSettings = _configuration.GetSection("ApiSettings").Get<ApiSettings>();

            ArgumentHelpers.ThrowIf.IsNull(apiSettings, nameof(apiSettings));

            _httpRequestPolicies = new HttpRequestPolicyHolder(_logger);

            _rabbitMqPublisherFactory = rabbitMqPublisherFactory;
        }

        /// <summary>
        /// Gather all of the status, EOB, and configuration required to create documents for a file and queue the data.
        /// This is what would be called instead of Step01
        /// </summary>
        /// <param name="statusFilePath">STS file to kick off process.</param>
        public async Task ParseAndQueueFile(string statusFilePath)
        {
            await ParseAndQueueTransactions(statusFilePath, null, false);
        }

        /// <summary>
        /// Gather all of the status, EOB, and configuration required to create documents for a file and queue the data.
        /// This is what would be called instead of Step01
        /// </summary>
        /// <param name="statusFilePath">STS file to kick off process.</param>
        /// <param name="transactionIds">Transactions from the STS file to enqueue for (re)generation.</param>
        /// <param name="type">
        /// <see cref="ParseType.Default"/> if the entire status file is to be run,
        /// <see cref="ParseType.Regenerate"/> if this is a regeneration request,
        /// <see cref="ParseType.Partial"/> if the creation of DocumentTransaction records and queuing of Distribution messages should be skipped.</param>
        public async Task ParseAndQueueTransactions(string statusFilePath, IList<long> transactionIds, bool isRegenRequest)
        {
            _logger.LogInformation("Start Processing {FilePath} - {Transactions} - {RegenRequest}", statusFilePath.Replace(Environment.NewLine, ""),
                transactionIds == null ? "" : string.Join(',', transactionIds), isRegenRequest);

            var statusFileInfo = ValidateStatusFiles(statusFilePath, transactionIds);

            foreach (var info in statusFileInfo)
            {
                if (isRegenRequest && string.IsNullOrEmpty(info.StatusFileName))
                {
                    throw new FileNotFoundException("Empty StatusFileName", info.StatusFilePath);
                }

                _logger.LogInformation("Start Processing {FilePath}", info.StatusFilePath.Replace(Environment.NewLine, ""));

                try
                {
                    using (_logger.BeginScope(new Dictionary<string, object>()
                    {
                        {"StatusFileName", info.StatusFileName}
                    }))
                    {
                        var isEmergencyFile = info.StatusFileName.IsEmergencyFile();

                        _logger.LogInformation("STS Parse Complete.");

                        // remove records where DistType 'U'
                        info.StatusFile.Records.RemoveAll(r => r.DIST == DistType.Unload);

                        info.StatusFile.Header.Count = info.StatusFile.Records.Count;

                        //Some re-issue files don't have the input file field set. We need it for document filenames later
                        if (string.IsNullOrEmpty(info.StatusFile.Header.InputFile))
                        {
                            info.StatusFile.Header.InputFile = Path.GetFileNameWithoutExtension(info.StatusFileName).Split('_')[1];
                        }

                        var tpa = info.StatusFile.GetTPA();
                        var originCode = info.StatusFile.GetOrigin();
                        var billingEntity = (info.StatusFile?.Records?.FirstOrDefault()?.BILENT ?? string.Empty).ToUpper();
                        var statusFileId = Received(info.StatusFileName, info.StatusFileDirectory, tpa, originCode, billingEntity);
                        var isRedCard = info.StatusFile.Header.Destination.Trim().Equals("RC", StringComparison.InvariantCultureIgnoreCase);
                        var isReIssue = info.StatusFileName.IsReIssueFile();

                        if (info.StatusFile.Records.Count == 0)
                        {
                            Ignored(statusFileId);
                            _logger.LogInformation("File ignored - no records to process");
                            return;
                        }

                        if (originCode.IsPaymentEngineConfirmationProcessing(info.StatusFileName, _configuration))
                        {
                            await _paymentEngineConfirmationFileService.ProcessFile(
                                new PaymentEngineConfirmationMessage { FilePath = info.StatusFilePath });

                            // Determine if this is a PE Document generation enabled for this billing entity
                            _logger.LogInformation("Checking Generate Document Status");
                            GenerateDocumentStatusDto? result;
                         
                            result = await _platformConfigurationApi.GetGenerateDocumentStatus(originCode, tpa, billingEntity).ConfigureAwait(false);

                            if (result != null)
                            {
                                GenerateDocumentStatusDto dto = new()
                                {
                                    OriginCode = result.OriginCode,
                                    ClientCode = result.ClientCode,
                                    BillingEntityCode = result.BillingEntityCode ?? string.Empty,
                                    IsGenerateDocumentStatus = result.IsGenerateDocumentStatus
                                };
                                // Check if isGenerateDocumentStatus is not "true"
                                if (dto.IsGenerateDocumentStatus != "true")
                                {
                                    _logger.LogInformation("Generate Document Status is not set to true; returning without further processing");
                                    return; // Return early as before
                                }
                            }
                            else
                            {
                                return;// Return early as before if result is null
                            }
                            _logger.LogInformation("Generate Document Status is set to true; continuing processing");
                        }

                        //Collect system level and TPA level configuration data
                        var tpaConfig = _docSysConfig.ReadConfig(tpa.ToLower());
                        _logger.LogInformation("Config Parse Complete.");

                        var isCustomSendBackTpa = await _platformConfigurationApi
                            .IsCustomSendBackEnabledForClient(tpa)
                            .ConfigureAwait(false);

                        using (_logger.BeginScope(new Dictionary<string, object>()
                        {
                            {"FileId", statusFileId},
                            {"TPA", tpa},
                            {"RedCard", isRedCard},
                            {"CustomSendBackTPA", isCustomSendBackTpa},
                        }))
                        {
                            if (isRegenRequest)
                            {
                                // we need to validate that this list of transactionIds is valid for the given statusFile.
                                // If this is not "valid" it will throw an error, to be caught by the calling process
                                tpaConfig.EnvironmentSettings.TryGetValue("indemnity_check_type", out var indemnityCheckType);
                                await ValidateForRegeneration(info.StatusFile, info.TransactionIds, indemnityCheckType);
                                _tracker.CleanTransactionStatus(info.TransactionIds);
                            }

                            //Drop to print
                            if (info.StatusFileName.IsDropToPrintFile())
                            {
                                await CallDropToPrint(info.StatusFilePath);
                                return;
                            }

                            var messages = await _documentTransactionMessageListBuilder.BuildMessagesAsync(
                                tpaConfig: tpaConfig,
                                statusFile: info.StatusFile,
                                statusFilePath: info.StatusFilePath,
                                transactionIds: info.TransactionIds,
                                isRegenRequest: isRegenRequest,
                                isReIssue: isReIssue,
                                isCustomSendBackTpa: isCustomSendBackTpa,
                                isRedCard: isRedCard);

                            // Insert DocumentTransaction records to the database and set status to Parsed
                            SetParsedStatus(messages, statusFileId, isRegenRequest);

                            // Create StpPayment records in database for STP payments for PRS/SDS EDI 835 types and B2B/C2B enrollment types.
                            await CreateStpPaymentRecords(info.StatusFile, originCode, tpa, billingEntity);

                            if (isRedCard)
                            {
                                _logger.LogInformation("Initiating RedCardFileStatus and RedCardTransactionStatus records for {FilePath}", info.StatusFilePath.Replace(Environment.NewLine, ""));
                                await _redCardRepository.InitializeFileStatus(statusFileId);
                            }
                            if (!isReIssue && !isRegenRequest)
                            {
                                await ExtractEdi835Docs(messages);
                            }
                            PublishMessages(messages, isRedCard, isEmergencyFile, info.StatusFileDirectory);

                            Queued(statusFileId);

                            if (!isRegenRequest)
                            {
                                await PublishDistributionMessage(info.StatusFilePath, info.StatusFile, statusFileId, isRedCard, isReIssue, isCustomSendBackTpa);
                            }

                            _logger.LogInformation("Messages Published.");
                        }
                    }
                }
                catch (Exception ex) when (LogError(ex))
                {
                    throw;
                }

                _logger.LogInformation("Completed Processing {FilePath}", info.StatusFilePath.Replace(Environment.NewLine, ""));
            }
        }

        /// <summary>
        /// Using the STS file passed regenerate the aux file
        /// </summary>
        /// <param name="statusFilePath">STS file to kick off process.</param>
        public async Task RegenerateAuxFile(string statusFilePath)
        {
            _logger.LogInformation("Start Regenerating Aux file from {FilePath}", statusFilePath?.Replace(Environment.NewLine, ""));

            var statusFileInfo = new StatusFileInfo(_statusFilePathHelper, statusFilePath);

            if (statusFileInfo.StatusFileName.IsDropToPrintFile())
            {
                throw new NotSupportedException("Drop to print files are not supported");
            }

            try
            {
                using (_logger.BeginScope(new Dictionary<string, object>()
                {
                    {"StatusFileName", statusFileInfo.StatusFileName}
                }))
                {
                    _logger.LogInformation("STS Parse Complete.");

                    // remove records where DistType 'U'
                    statusFileInfo.StatusFile.Records.RemoveAll(r => r.DIST == DistType.Unload);

                    statusFileInfo.StatusFile.Header.Count = statusFileInfo.StatusFile.Records.Count;

                    //Some re-issue files don't have the input file field set. We need it for document filenames later
                    if (string.IsNullOrEmpty(statusFileInfo.StatusFile.Header.InputFile))
                    {
                        statusFileInfo.StatusFile.Header.InputFile =
                            Path.GetFileNameWithoutExtension(statusFileInfo.StatusFileName).Split('_')[1];
                    }

                    var isRedCard = statusFileInfo.StatusFile.Header.Destination.Trim().Equals("RC", StringComparison.InvariantCultureIgnoreCase);
                    var isReIssue = statusFileInfo.StatusFileName.IsReIssueFile();

                    if (statusFileInfo.StatusFile.Records.Count == 0)
                    {
                        _logger.LogInformation("File ignored - no records to process");
                        return;
                    }

                    if (!isRedCard)
                    {
                        throw new NotSupportedException("Status file must be a red card document");
                    }

                    _logger.LogInformation($"Initiating .AUX creation for {statusFilePath.Replace(Environment.NewLine, "")}");
                    var trackerInfo = _tracker.GetStatusFileSummary(statusFilePath);
                    if (trackerInfo == null || trackerInfo.Count == 0)
                    {
                        throw new FileNotFoundException($"Record not found in DB {statusFilePath}");
                    }

                    if (!trackerInfo.TryGetValue("PaymentStatusFileId", out var statusFileIdObject))
                    {
                        throw new KeyNotFoundException("PaymentStatusFileId");
                    }

                    var statusFileId = statusFileIdObject switch
                    {
                        long valueAsLong => valueAsLong,
                        int valueAsInt => valueAsInt,
                        _ => throw new KeyNotFoundException("PaymentStatusFileId")
                    };

                    await CreateAuxiliaryFile(statusFileInfo.StatusFile, statusFileInfo.StatusFileName, statusFileId, isReIssue, false, false);
                }
            }
            catch (Exception ex) when (LogError(ex))
            {
                throw;
            }

            _logger.LogInformation("Completed Regenerating Aux file from {FilePath}", statusFilePath.Replace(Environment.NewLine, ""));
        }

        /// <summary>
        /// Determine if there are any 835 documents to be created and
        /// save into the database for later.
        /// </summary>
        private async Task ExtractEdi835Docs(List<DocumentTransactionMessage> messages)
        {
            if (!_enableEdi835Parsing)
            {
                _logger.LogInformation("Edi835 parsing is disabled.");
                return;
            }

            using (_logger.BeginScope(new Dictionary<string, object>()
                {
                    {"StatusFileName", messages?.First()?.StatusFilePath??"" }
                }))
            using (new PerfTimerLogger(_logger, "Edi835TarService.ExtractDocs"))
            {
                _logger.LogInformation("Starting Edi835TarService.ExtractDocs");
                await _edi835TarService.ExtractDocs(messages, CancellationToken.None);
            }
            return;
        }

        public Task QueueFileForParsing(string statusFilePath, IList<long> transactionIds, bool isRegenRequest, bool isEmergencyFile = false)
        {
            // Get the "parsing queue" and put the file on the
            //Bind properties of Queue in app settings to QueueConfigurationSettings
            QueueConfigurationSettings queueSettings = new QueueConfigurationSettings();
            _configuration.Bind("ParsingQueue", queueSettings);
            ParsingMessage msg = new ParsingMessage
            {
                StatusFilePath = statusFilePath,
                TransactionIds = transactionIds,
                IsRegenRequest = isRegenRequest,
            };

            using (var msgQueue = _rabbitMqPublisherFactory.GetPublisher(queueSettings, _loggerMessagePub))
            {
                if (isEmergencyFile)
                {
                    //Leave room for higher priority in the future, but SWK 1hr turnaround should be close to highest
                    msgQueue.Publish(JsonConvert.SerializeObject(msg));
                }
                else
                {
                    msgQueue.Publish(JsonConvert.SerializeObject(msg));
                }

                _logger.LogDebug("Queued statusFilePath={statusFilePath}, isRegenRequest={isRegenRequest}, transactionIds={transactionIds}", msg.StatusFilePath.Replace(Environment.NewLine, ""),
                    msg.IsRegenRequest,
                    msg.TransactionIds == null ? "" :
                        string.Join(",", msg.TransactionIds.Select(t => t.ToString()).ToArray()));
            }

            return Task.CompletedTask;
        }

        private bool LogError(Exception ex)
        {
            _logger.LogError(ex, $"An unexpected exception occurred: [{ex.Message}]");
            return true;
        }

        private void SetParsedStatus(List<DocumentTransactionMessage> messages, int fileId, bool isRegenRequest)
        {
            if (!isRegenRequest)
            {
                Parsed(fileId, messages);
            }
            else
            {
                _tracker.UpdateFileStatus(fileId, (int)FileStatus.Parsed);
            }
        }

        private void PublishMessages(List<DocumentTransactionMessage> messages, bool isRedCard, bool isEmergencyFile, string statusFileDirectory)
        {
            //Adds the EOB XML and/or PDF EOB data
            if (messages.Any(m => m.GenerateEOB))
            {
                //NOTE: Red Card Check Info is also set at this time
                _tarService.ParseTarAndPublishMessages(messages, statusFileDirectory, isRedCard, isEmergencyFile, _configuration.GetSection("StatusFile").GetValue<bool>("SaveMessages"));
            }
            else
            {
                //Bind properties of Queue in app settings to QueueConfigurationSettings
                QueueConfigurationSettings queueSettings = new QueueConfigurationSettings();
                _configuration.Bind("DocumentQueue", queueSettings);

                using (var msgQueue = _rabbitMqPublisherFactory.GetPublisher(queueSettings, _loggerMessagePub))
                {
                    foreach (var msg in messages)
                    {
                        if (isEmergencyFile)
                        {
                            msgQueue.PublishUsingMessageRepository<DocumentTransactionMessage>(msg);
                        }
                        else
                        {
                            msgQueue.PublishUsingMessageRepository<DocumentTransactionMessage>(msg);
                        }

                        _logger.LogDebug("Queued  Guid = {GUID} TransactionId = {TransactionId} RefCode = {RefCode}", msg.GUID, msg.StatusRecord.TXID.RemoveLeadingZeroes(), msg.StatusRecord.TRANID);
                    }
                }
            }

            _logger.LogInformation("Tar Parse Complete.");
        }

        private IEnumerable<StatusFileInfo> ValidateStatusFiles(string statusFilePath, IList<long> transactionIds)
        {
            // .sts file path is given

            if (!string.IsNullOrEmpty(statusFilePath))
            {
                var info = new StatusFileInfo(_statusFilePathHelper, statusFilePath);

                // transaction ids from .sts file

                var stsTransactionIds = info.StatusFile.Records
                    .Select(r => long.Parse(r.TXID.RemoveLeadingZeroes()))
                    .ToList();

                // specific transactions ids are given

                if (transactionIds?.Count > 0)
                {
                    info.TransactionIds.AddRange(stsTransactionIds.Intersect(transactionIds));

                    if (info.TransactionIds.Count != transactionIds.Count)
                    {
                        throw new KeyNotFoundException(
                            $"Unable to find TransactionIds in StatusFile: [{string.Join(",", transactionIds.Except(info.TransactionIds))}]");
                    }
                }
                else
                {
                    info.TransactionIds.AddRange(stsTransactionIds);
                }

                return new List<StatusFileInfo> { info };
            }

            // .sts file path is not given

            var stsDictionary = new Dictionary<string, StatusFileInfo>();

            foreach (var transactionId in transactionIds)
            {
                var stsFilePath = _tracker.GetStatusFileNameFromTransaction(transactionId);

                if (string.IsNullOrEmpty(stsFilePath))
                {
                    throw new KeyNotFoundException($"Unable to find the StatusFileName for TransactionId: {transactionId}");
                }

                if (!stsDictionary.ContainsKey(stsFilePath))
                {
                    stsDictionary.Add(stsFilePath, new StatusFileInfo(_statusFilePathHelper, stsFilePath));
                }

                stsDictionary[stsFilePath].TransactionIds.Add(transactionId);
            }

            return stsDictionary.Values;
        }

        private async Task ValidateForRegeneration(StatusFile statusFile, IList<long> transactionIds, string indemnityCheckType)
        {
            var allTxIdsInSts = await Task.FromResult<List<long>>(statusFile.Records.Select(sts => long.Parse(sts.TXID.RemoveLeadingZeroes())).ToList());

            var txFound = transactionIds.Intersect(allTxIdsInSts).ToList();
            // check that all transactionIds exist in the given file
            if (!transactionIds.All(_ => txFound.Contains(_)))
            {
                throw new InvalidForRegenerationException("Not all SETXIDs found in status file");
            }

            var distinctPackages = statusFile.Records
                .Where(_ => allTxIdsInSts.Contains(long.Parse(_.TXID.RemoveLeadingZeroes())))
                .Select(_ => $"{_.PRTVENDOR}_{_.TPA}_{statusFile.Header.InputFile}_{_.PAYTYPE}.zip")
                .Distinct().ToList();
            if (!string.IsNullOrEmpty(indemnityCheckType))
            {
                distinctPackages.AddRange(
                    statusFile.Records
                        .Where(_ => _.PAYTYPE == "CLCHK" && allTxIdsInSts.Contains(long.Parse(_.TXID.RemoveLeadingZeroes())))
                        .Select(_ => $"{_.PRTVENDOR}_{_.TPA}_{statusFile.Header.InputFile}_{indemnityCheckType}.zip")
                        .Distinct());
            }
        }

        private async Task PublishDistributionMessage(string statusFilePath, StatusFile statusFile, long statusFileID, bool isRedCard, bool isReIssue, bool isCustomSendBackTpa)
        {
            if (isRedCard)
            {
                await this.PublishRedCardDistributionMessage(statusFilePath, statusFile, statusFileID, isReIssue, isCustomSendBackTpa);
            }
            else
            {
                this.PublishDistributionMessage(statusFilePath, statusFile, statusFileID, isCustomSendBackTpa);
            }
        }

        private void PublishDistributionMessage(string statusFilePath, StatusFile statusFile, long statusFileID, bool isCustomSendBackTpa)
        {
            var queueSettings = new QueueConfigurationSettings();

            _configuration.Bind("DistributionQueue", queueSettings);

            using (var msgQueue = _rabbitMqPublisherFactory.GetPublisher(queueSettings, _loggerMessagePub))
            {
                msgQueue.Publish(CreateDistributionMessage(statusFilePath, statusFile, statusFileID, isCustomSendBackTpa, DefaultDistributionWarningDelayInHours));
                _logger.LogDebug("Distribution for status file ID {fileId} queued.", statusFileID);
            }
        }

        private async Task PublishRedCardDistributionMessage(string statusFilePath, StatusFile statusFile, long statusFileID, bool isReIssue, bool isCustomSendBackTpa)
        {
            _logger.LogInformation($"Initiating .AUX creation for {statusFilePath.Replace(Environment.NewLine, "")}");

            await CreateAuxiliaryFile(statusFile, statusFilePath, statusFileID, isReIssue);

            var queueSettings = new QueueConfigurationSettings();

            _configuration.Bind("RedCardDistributionQueue", queueSettings);

            // send all transactions to RC Dist Queue
            using (var msgQueue = _rabbitMqPublisherFactory.GetPublisher(queueSettings, _loggerMessagePub))
            {
                msgQueue.Publish(CreateDistributionMessage(statusFilePath, statusFile, statusFileID, isCustomSendBackTpa, RedCardDistributionWarningDelayInHours));
                _logger.LogDebug("RedCard Distribution for status file ID {fileId} queued.", statusFileID);
            }

            // if isCustomSendBackTPA, send all S transactions to DocDistQueue for custom send back distribution
            if (isCustomSendBackTpa && statusFile.Records.Any(r => r.DIST == "S"))
            {
                statusFile.Records.RemoveAll(r => r.DIST != "S");

                queueSettings = new QueueConfigurationSettings();

                _configuration.Bind("DistributionQueue", queueSettings);

                using (var msgQueue = _rabbitMqPublisherFactory.GetPublisher(queueSettings, _loggerMessagePub))
                {
                    msgQueue.Publish(CreateDistributionMessage(statusFilePath, statusFile, statusFileID, isCustomSendBackTpa, DefaultDistributionWarningDelayInHours));
                    _logger.LogDebug("Custom Send Back Distribution for status file ID {fileId} queued.", statusFileID);
                }
            }
        }

        public async Task<string> GenerateDistributionMessage(string statusFilePath, string distType)
        {
            var validStsPath = _statusFilePathHelper.CleanAndValidateStsPath(statusFilePath)
                ?? throw new Exception("Invalid status file path, either the path was not to a file or it did not start with /SRVFS/bogner/transfer");

            var statusFile = new StatusFile(File.ReadLines(validStsPath).ToArray());

            if (distType == "M")
            {
                statusFile.Records.RemoveAll(r => r.DIST == "F" || r.DIST == "R");
            }
            else if (distType == "F")
            {
                statusFile.Records.RemoveAll(r => r.DIST != "F" && r.DIST != "R");
            }
            else if (distType == "S")
            {
                statusFile.Records.RemoveAll(r => r.DIST != "S");
            }

            var statusFileId = _tracker.GetFileId(validStsPath);

            var isCustomSendBackTpa = await _platformConfigurationApi
                .IsCustomSendBackEnabledForClient(statusFile.GetTPA())
                .ConfigureAwait(false);

            return CreateDistributionMessage(
                validStsPath,
                statusFile,
                statusFileId,
                isCustomSendBackTpa,
                DefaultDistributionWarningDelayInHours);
        }

        private string CreateDistributionMessage(string statusFilePath, StatusFile statusFile, long statusFileID, bool isCustomSendBackTpa, int warningDelayInHours)
        {
            return JsonConvert.SerializeObject(_distributionMessageBuilder.CreateDistributionMessage(
                statusFile,
                statusFilePath,
                statusFileID,
                isCustomSendBackTpa,
                warningDelayInHours));
        }

        private async Task CreateAuxiliaryFile(StatusFile file, string fileName, long fileId, bool isReIssue, bool checkAuxFileGenerated = true, bool updateStatus = true)
        {
            var auxFilePath = await _auxiliaryFileWriter.Write(fileName, fileId, file, isReIssue, checkAuxFileGenerated, updateStatus);

            _logger.LogInformation($"Auxiliary File {auxFilePath} created for {file.GetTPA()} with id: {fileId}");
        }

        private Task<bool> CallDropToPrint(string statusFile)
        {
            string url = _configuration.GetSection("StatusFile").GetValue<string>("DropToPrintURL");
            url += $"?statusFilePath={System.Uri.EscapeUriString(statusFile)}";
            HttpClient client = new HttpClient { BaseAddress = new Uri(url) };
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            return _httpRequestPolicies.HttpPostPolicy.ExecuteAsync(async () =>
            {
                HttpResponseMessage response = await client.PostAsJsonAsync(url, string.Empty);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApplicationException(await response.Content.ReadAsStringAsync());
                }
                return response.IsSuccessStatusCode;
            }, url);
        }

        private int Received(string fileName, string filePath, string tpa, string originCode, string billingEntity)
        {
            return _tracker.InsertStatusFile(fileName, filePath, tpa, originCode, billingEntity);
        }

        private void Parsed(int fileId, List<DocumentTransactionMessage> messages)
        {
            _tracker.InsertFileTransactions(fileId, messages);
            _tracker.UpdateFileStatus(fileId, (int)FileStatus.Parsed);
        }

        private void Queued(int fileId)
        {
            _tracker.UpdateFileStatus(fileId, (int)FileStatus.Queued);
        }

        private void Ignored(int fileId)
        {
            _tracker.UpdateFileStatus(fileId, (int)FileStatus.Ignored);
        }

        /// <inheritdoc/>
        public async Task<string> GenerateAndPublishDistributionMessage(string statusFilePath, string distType)
        {
            var message = await this.GenerateDistributionMessage(statusFilePath, distType);

            var statusFileId = _tracker.GetFileId(statusFilePath);

            var queueSettings = new QueueConfigurationSettings();
            _configuration.Bind("DistributionQueue", queueSettings);

            using (var msgQueue = _rabbitMqPublisherFactory.GetPublisher(queueSettings, _loggerMessagePub))
            {
                msgQueue.Publish(message);
                _logger.LogDebug("Distribution for status file ID {statusFileId} queued.", statusFileId);
            }

            return message;
        }

        /// <summary>
        /// Insert STP payment records into database table.
        /// STP payments are identified by having the Network Type of 'STP'.
        /// STP records are extracted from the Status File and grouped by Network Name and Enrollment Type.
        /// The EDI 835 type is determined by checking if the client listed in the Status File is for a PRS or SDS account.
        /// Each distinct group results in a separate bulk insert operation into the database.
        /// </summary>
        /// <param name="statusFile">The status file containing zero or more STP payment records.</param>
        /// <param name="originCode">The origin code from the status file.</param>
        /// <param name="clientCode">The client code from the status file.</param>
        /// <param name="billingEntityCode">The billing entity code from the status file.</param>
        public async Task<IEnumerable<long>> CreateStpPaymentRecords(StatusFile statusFile, string originCode, string clientCode, string billingEntityCode)
        {
            // Extract STP payment records from Status File (records with NetworkType = 'STP')
            // and group them by NetworkName and EnrollmentType
            var stpContent = new StatusFileStpContent(statusFile);

            // Keep track of the TransactionIds for which new STP payment records were inserted, to be returned at the end of the method.
            var insertedTransactionIds = new List<long>();

            // If no STP payments found then skip STP payment record creation.
            if (!stpContent.StpGroups.Any())
            {
                _logger.LogDebug("SKIP: Status File contains no STP records, skipping STP Payment record creation.");
                return insertedTransactionIds;
            }

            string fileName = stpContent.FileName;
            string edi835Type = await GetEdi835Type(originCode, clientCode, billingEntityCode).ConfigureAwait(false);

            // Insert STP payment records into database table using the grouped STP records from the Status File
            foreach (var stpGroup in stpContent.StpGroups)
            {
                var existingTransactionIds = _tracker.GetStpPayments(stpGroup.TransactionIds).Select(x => x.TransactionId);
                var newTransactionIds = stpGroup.TransactionIds.Except(existingTransactionIds).ToList();
                if (newTransactionIds.Count == 0)
                {
                    _logger.LogDebug("SKIP: Status File contains {Count} {EnrollmentType} STP records for {NetworkName}, all records already exist - {FileName}",
                        stpGroup.TransactionIds.Count(), stpGroup.EnrollType, stpGroup.NetworkName, fileName);
                    continue;
                }

                if (stpGroup.IsC2B())
                {
                    // Skip C2B STP payments, they do not need to be tracked for now.
                    _logger.LogDebug("SKIP: Status File contains {Count} {EnrollmentType} STP records for {NetworkName}, skipping records - {FileName}",
                        stpGroup.TransactionIds.Count(), stpGroup.EnrollType, stpGroup.NetworkName, fileName);
                    continue;
                }

                if (stpGroup.IsB2B())
                {
                    // Insert B2B STP payment records to the database.
                    _logger.LogDebug("CREATE: Status File contains {Count} {EnrollmentType} new STP records for {NetworkName}, inserting records - {FileName}",
                        newTransactionIds.Count, stpGroup.EnrollType, stpGroup.NetworkName, fileName);
                    _tracker.BulkInsertStpPayments(newTransactionIds, stpGroup.NetworkName, stpGroup.EnrollType, edi835Type);
                    insertedTransactionIds.AddRange(newTransactionIds);
                    continue;
                }
            }

            return insertedTransactionIds;
        }

        /// <summary>
        /// Get PRS configuration from Platform Config API for the given client, origin, and billing entity.
        /// </summary>
        /// <param name="clientCode">The client code from the status file.</param>
        /// <param name="originCode">The origin code from the status file.</param>
        /// <param name="billingEntity">The billing entity code from the status file.</param>
        /// <param name="paymentTypeCode">The optional payment type code.</param>
        /// <param name="configurationDate">The optional configuration date to check the validity of the configuration.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns></returns>
        public async Task<OptumPrsConfigurationDto> GetPrsConfiguration(
            string clientCode,
            string originCode,
            string billingEntity,
            string paymentTypeCode = null,
            DateTime? configurationDate = null,
            CancellationToken cancellationToken = default)
        {
            // Determine if this is a PRS account
            return await _platformConfigurationApi
                .GetOptumPrsConfiguration(clientCode, originCode, billingEntity, paymentTypeCode, configurationDate, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Determine if EDI 835 type is PRS or SDS based on the payment client information.
        /// </summary>
        /// <param name="originCode">The origin code from the status file.</param>
        /// <param name="clientCode">The client code from the status file.</param>
        /// <param name="billingEntityCode">The billing entity code from the status file.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The EDI 835 type: PRS or SDS.</returns>
        public async Task<string> GetEdi835Type(
            string originCode,
            string clientCode,
            string billingEntityCode,
            CancellationToken cancellationToken = default)
        {
            // Determine if this is a PRS account
            OptumPrsConfigurationDto prsConfig = await GetPrsConfiguration(clientCode, originCode, billingEntityCode, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var isPrs = prsConfig != null && prsConfig.IsPrsEnabled;
            var edi835Type = isPrs ? Edi835Types.PRS : Edi835Types.SDS;
            return edi835Type;
        }
    }
}
