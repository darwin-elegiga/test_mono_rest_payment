using System;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Models;
using VPay.Queuing;

namespace VPay.DocSys.Generation
{
    public class FaxMessageErrorQueueMessageProcessor : IFaxMessageErrorQueueMessageProcessor
    {
        private static readonly string CountKey = nameof(BaseFaxMessagePayload.ErrorRetryCount);

        private readonly QueueConfigurationSettings _queue;
        private readonly QueueConfigurationSettings _errorQueue;
        private readonly IQueueService _queueService;
        private readonly ILogger _logger;

        public FaxMessageErrorQueueMessageProcessor(
            QueueConfigurations queueConfigurations,
            IQueueService queueService,
            ILogger<FaxMessageErrorQueueMessageProcessor> logger)
        {
            ArgumentHelpers.ThrowIf.IsChildPropertyNull(queueConfigurations.FaxMessageQueue, nameof(queueConfigurations), nameof(queueConfigurations.FaxMessageQueue));

            ArgumentHelpers.ThrowIf.IsNull(queueService, nameof(queueService));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _queue = queueConfigurations.FaxMessageQueue;
            _errorQueue = queueService.GetErrorQueueConfiguration(_queue);
            _queueService = queueService;
            _logger = logger;
        }

        public void ProcessMessageString(string message, int maxRetryCount)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(message, nameof(message));

            var header = message.Substring(0, 275);
            var payloadString = message.Substring(275);

            try
            {
                // try to parse json
                var json = JsonConvert.DeserializeObject<JObject>(payloadString);
                ProcessMessage(header, json, maxRetryCount);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error occurred handling an Error Queue Fax message {Message} will wrap json payload and requeue", e.Message);

                WrapPayloadAndMaximizeErrorCount(header, payloadString, maxRetryCount);
            }
        }

        private void ProcessMessage(string header, JObject json, int maxRetryCount)
        {

            using (_logger.BeginScope(json.ToDictionaryExcept("xml", "BadPayload", "tids")))
            {
                _logger.LogDebug("Start Error Retry Count");

                var errorRetryCount = json.Value<int>(CountKey);

                // always update to keep correct count
                errorRetryCount++;
                json[CountKey] = errorRetryCount;
                _logger.LogDebug("Set Error Retry Count x{ErrorRetryCount}", errorRetryCount);

                var newMessage = $"{header}{json}";

                if (!int.TryParse(newMessage.Substring(73, 10), out var priorityInt))
                {
                    throw new Exception("invalid priority when trying to requeue");
                }
                // hit max count, keep it in the error queue by put in the back of the queue
                if (errorRetryCount > maxRetryCount)
                {
                    // move to the back of the line
                    _queueService.PublishQueueMessage(_errorQueue, newMessage);
                }
                else
                {
                    // put back on normal queue
                    var rmqPriority = priorityInt.ConvertAndScalePriority(100, _queue.MaxPriority);

                    _logger.LogInformation("Move message to {QueueName} with {ErrorRetryCount} retry count", _queue.QueueName, errorRetryCount);
                    _queueService.PublishQueueMessage(_queue, newMessage, rmqPriority);
                }
            }
        }

        private void WrapPayloadAndMaximizeErrorCount(string header, string payloadString, int maxRetryCount)
        {
            var newJson = new JObject
            {
                // set to maxRetryCount
                [CountKey] = maxRetryCount,
                // wrap bad json
                ["BadPayload"] = JsonConvert.SerializeObject(payloadString),
            };

            _logger.LogDebug("Set Error Retry Count x{ErrorRetryCount}", maxRetryCount);

            // keep in the queue
            _queueService.PublishQueueMessage(_errorQueue, $"{header}{newJson}");
        }
    }
}
