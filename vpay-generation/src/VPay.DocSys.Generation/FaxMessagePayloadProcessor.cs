using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Generation.Models;
using VPay.Queuing;

namespace VPay.DocSys.Generation
{
    public class FaxMessagePayloadProcessor : IFaxMessagePayloadProcessor
    {
        private readonly QueueConfigurationSettings _faxQueueConfigurationSettings;
        private readonly IQueueService _queueService;
        private readonly ILogger _logger;

        public FaxMessagePayloadProcessor(
            QueueConfigurations queueConfigurations,
            IQueueService queueService,
            ILogger<FaxMessagePayloadProcessor> logger)
        {
            ArgumentHelpers.ThrowIf.IsChildPropertyNull(queueConfigurations.FaxMessageQueue, nameof(queueConfigurations), nameof(queueConfigurations.FaxMessageQueue));
            ArgumentHelpers.ThrowIf.IsNull(queueService, nameof(queueService));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _faxQueueConfigurationSettings = queueConfigurations.FaxMessageQueue;
            _queueService = queueService;
            _logger = logger;
        }

        public void ProcessPayload(FaxMessagePayloadRequest payload)
        {
            ArgumentHelpers.ThrowIf.IsNull(payload, nameof(payload));

            using (_logger.BeginScope(payload.ToDictionaryExcept(nameof(payload.VPaygeXmlBase64))))
            {
                var message = new FaxMessageQueueMessage(payload);

                _queueService.PublishQueueMessage(_faxQueueConfigurationSettings, message.ToString());
                _logger.LogInformation("Published PMB Message for Fax Queue");
            }
        }
    }
}
