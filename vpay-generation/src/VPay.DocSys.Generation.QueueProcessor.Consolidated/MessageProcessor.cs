using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VPay.DocSys.Common;
using VPay.DocSys.Common.RabbitMQ;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Rabbitmq;
using VPay.DocSys.Generation.Models;
using VPay.Queuing;

namespace VPay.DocSys.Generation.QueueProcessor.Consolidated
{
    public class MessageProcessor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly QueueConfigurations _queueConfigurations;
        private int _processCounter = 0;
        private readonly ProcessorSettings _processorSettings;
        private readonly QueueConfigurationSettings _queueConfigurationSettings;
        private readonly ILogger _logger;

        private IMessageRepositoryReceiver _queueReceiver;
        private readonly IMessageRepositoryFactory _messageRepositoryFactory;

        private bool _isProcessingMessage;

        public MessageProcessor(
            IServiceProvider serviceProvider,
            ILogger<MessageProcessor> logger,
            IMessageRepositoryFactory messageRepositoryFactory)
        {
            _serviceProvider = serviceProvider;
            using (var scope = _serviceProvider.CreateScope())
            {
                _processorSettings = scope.ServiceProvider
                            .GetRequiredService<ProcessorSettings>();
                _queueConfigurations = scope.ServiceProvider
                            .GetRequiredService<QueueConfigurations>();
                _queueConfigurationSettings = _queueConfigurations.DocumentGenerationQueue;
            }
            _logger = logger;
            _messageRepositoryFactory = messageRepositoryFactory;
        }

        public void Start()
        {
            using (_logger.BeginScope(new Dictionary<string, object>()
            {
                { "QueueName", _queueConfigurationSettings.QueueName },
                { "ErrorQueueName", _queueConfigurationSettings.ErrorQueueName },
                { "Exchange", _queueConfigurationSettings.Exchange },
                { "Host", _queueConfigurationSettings.Host },
                { "VirtualHost", _queueConfigurationSettings.VirtualHost },
                { "UserName", _queueConfigurationSettings.UserName },
                { "Port", _queueConfigurationSettings.Port },
                { "MaxPriority", _queueConfigurationSettings.MaxPriority },
            }))
            {
                _logger.LogInformation($"{this.GetType().FullName} starting.");
            }

            _queueReceiver = (IMessageRepositoryReceiver)QuorumReceiverFactory.GetReceiver(_queueConfigurationSettings, async (string s) => await this.ProcessMessage(s), _logger);
            _queueReceiver.UseMessageRepository<DocumentTransactionMessage>(_messageRepositoryFactory);
            _queueReceiver.Consume();
        }

        public void Stop()
        {
            while (this._isProcessingMessage)
            {
                Thread.Sleep(10);
            }

            _queueReceiver.Stop();
        }

        public async Task<bool> ProcessMessage(string message)
        {
            this._isProcessingMessage = true;
            if (++_processCounter > _processorSettings.GarbageCollectionInterval)
            {
                GC.Collect();
                _processCounter = 0;
            }

            var payload = PreparePayload(message);
            string additionalInfo = $"{{InputFile}}={payload?.InputFile ?? "NULL"} {{TXID}}={payload?.StatusRecord?.TXID.RemoveLeadingZeroes() ?? "NULL"}";

            using (BeginLogScope(payload))
            using (new PerfTimerLogger(_logger, $"Executed method GenerateAllDocs - {additionalInfo}"))
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    try
                    {
                        var scopedProcessingService =
                            scope.ServiceProvider
                               .GetRequiredService<IDocSysOperations>();

                        _logger.LogInformation($"Started Executing method GenerateAllDocs - {additionalInfo}");
                        await scopedProcessingService.GenerateAllDocs(payload);

                        this._isProcessingMessage = false;
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, $"An error occurred generating documents : {exception.Message}");
                        // The entire callstack was being swallowed by throwing a "new" exception.  Throw by itself will preserve the callstack.
                        throw;
                    }
                }
            }
            return true;
        }

        private IDisposable BeginLogScope(DocGenParam payload)
        {
            return _logger.BeginScope(new Dictionary<string, object>
            {
                { "TPA",  payload.StatusRecord.TPA },
                { "StageFileKey",  payload.InputFile },
                { "TransactionId",  payload.StatusRecord.TXID.RemoveLeadingZeroes() },
                { "GUID",  payload.StatusRecord.GUID },
                { "RefCode",  payload.StatusRecord.TRANID }
            });
        }

        public DocGenParam PreparePayload(string message)
        {
            var msgContent = JsonConvert.DeserializeObject<dynamic>(message);
            var configData = JsonConvert.DeserializeObject<DocSysConfiguration>(msgContent.ConfigData.ToString());
            OtbRecord otbData = JsonConvert.DeserializeObject<OtbRecord>(msgContent.OTBData.ToString());
            CheckInfo checkInfo = msgContent.CheckInfo == null ? null : JsonConvert.DeserializeObject<CheckInfo>(msgContent.CheckInfo.ToString(), JsonHelpers.DefaultSnakeCaseJsonSerializationSettings());
            SubstituteDataInfo substituteDataInfo = msgContent.SubstituteDataInfo == null ? null : JsonConvert.DeserializeObject<SubstituteDataInfo>(msgContent.SubstituteDataInfo.ToString());

            bool regen = (message.Contains("RegeneratePackage") && (bool)msgContent.RegeneratePackage);

            EOB eobData = JsonConvert.DeserializeObject<EOB>(msgContent.EOBData.ToString());
            var apiParam = new DocGenParam()
            {
                StatusFilePath = msgContent.StatusFilePath,
                RegeneratePackage = regen,
                Configuration = configData,
                CheckInfo = checkInfo,
                EOBData = eobData,
                OtbRecord = otbData,
                StatusRecord = JsonConvert.DeserializeObject<StatusRecord>(msgContent.StatusRecord.ToString()),
                GenerateEOB = msgContent.GenerateEOB,
                InputFile = msgContent.InputFile,
                CDate = msgContent.CDate,
                SubstituteDataInfo = substituteDataInfo,
                IsRedCard = msgContent.IsRedCard,
                IsCustomSendBack = msgContent.IsCustomSendBack ?? false
            };
            return apiParam;
        }
    }
}
