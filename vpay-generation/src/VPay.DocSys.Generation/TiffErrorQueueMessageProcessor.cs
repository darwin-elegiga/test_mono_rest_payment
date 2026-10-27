using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Models;
using VPay.Queuing;

namespace VPay.DocSys.Generation
{
    public class TiffErrorQueueMessageProcessor : ITiffErrorQueueMessageProcessor
    {
        private readonly QueueConfigurationSettings _queue;
        private readonly QueueConfigurationSettings _errorQueue;
        private readonly IQueueService _queueService;
        private readonly ILogger _logger;

        public TiffErrorQueueMessageProcessor(
            QueueConfigurations queueConfigurations,
            IQueueService queueService,
            ILogger<TiffErrorQueueMessageProcessor> logger)
        {
            ArgumentHelpers.ThrowIf.IsChildPropertyNull(queueConfigurations.TiffQueue, nameof(queueConfigurations), nameof(queueConfigurations.TiffQueue));

            ArgumentHelpers.ThrowIf.IsNull(queueService, nameof(queueService));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _queue = queueConfigurations.TiffQueue;

            _errorQueue = queueService.GetErrorQueueConfiguration(_queue);
            _queueService = queueService;
            _logger = logger;
        }

        public async Task ProcessMessageString(string message, int maxRetryCount)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(message, nameof(message));

            var tiffQueueMessage = JsonConvert.DeserializeObject<TiffQueueMessage>(message);
            var logDictionary = tiffQueueMessage.ToDictionary();
            logDictionary[nameof(tiffQueueMessage.TransactionIds)] = string.Join(", ", tiffQueueMessage.TransactionIds);

            using (_logger.BeginScope(logDictionary))
            {
                try
                {
                    _logger.LogDebug("Start Error Retry Count");

                    // always update to keep correct count
                    tiffQueueMessage.ErrorRetryCount++;
                    var errorRetryCount = tiffQueueMessage.ErrorRetryCount;
                    _logger.LogDebug("Set Error Retry Count x{ErrorRetryCount}", tiffQueueMessage.ErrorRetryCount);

                    var newMessage = JsonConvert.SerializeObject(tiffQueueMessage);

                    // hit max count, keep it in the error queue by put in the back of the queue
                    if (errorRetryCount > maxRetryCount)
                    {
                        // move to the back of the line
                        _queueService.PublishQueueMessage(_errorQueue, newMessage);
                    }
                    else
                    {
                        // put back on normal queue
                        _logger.LogInformation("Move message to {QueueName} with {ErrorRetryCount}", _queue.QueueName, errorRetryCount);
                        _queueService.PublishQueueMessage(_queue, newMessage, tiffQueueMessage.Priority);
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogCritical(exception, "An error occurred handling an Error Queue Tiff message {Message} will requeue", exception.Message);

                    _queueService.PublishQueueMessage(_errorQueue, message);
                }

                await Task.CompletedTask;
            }
        }
    }
}
