using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Models;
using VPay.Queuing;
using static VPay.DocSys.Common.ArgumentHelpers;
using DS = VPay.DocSys.Data;

namespace VPay.DocSys.Generation
{
    public class Edi835MessagePublisher : IEdi835MessagePublisher
    {
        private readonly IEdi835Repository _edi835Repository;
        private readonly IMessagePublisherService _messagePublisherService;
        private readonly QueueConfigurations _queueConfigurations;
        private readonly QueueConfigurationSettings _edi835GenerationQueueConfigurationSettings;
        private readonly ILogger<Edi835MessagePublisher> _logger;
        private const string _nl = "\n"; // Avoid the dev on Windows/ build/test/run on Linux line ending issue.


        public Edi835MessagePublisher(
            IEdi835Repository edi835Repository,
            IMessagePublisherService messagePublisherService,
            QueueConfigurations queueConfigurations,
             ILogger<Edi835MessagePublisher> logger)
        {

            _edi835Repository = edi835Repository ?? throw new ArgumentNullException(nameof(edi835Repository));
            _messagePublisherService = messagePublisherService ?? throw new ArgumentNullException(nameof(messagePublisherService));
            _queueConfigurations = queueConfigurations ?? throw new ArgumentNullException(nameof(queueConfigurations));
            _edi835GenerationQueueConfigurationSettings = queueConfigurations.Edi835GenerationQueue ?? throw new ArgumentNullException(nameof(queueConfigurations.Edi835GenerationQueue));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task PublishEdi835Message(long transactionId, CancellationToken cancellationToken)
        {

            var edi835 = await _edi835Repository.GetMostRecent835(transactionId, cancellationToken);
            if (!(edi835?.IsTerminalStatus ?? true) && edi835.StatusCode == Edi835StatusCode.Initiated)
            {
                await PublishMessage(edi835, cancellationToken);
            }
        }

        private async Task PublishMessage(DS.Edi835 message, CancellationToken cancellationToken)
        {

            using (_logger.BeginScope(message.ToDictionary()))
            {
                var wrappedMessage = JsonConvert.SerializeObject(new MessageWrapper<DS.Edi835>(message));
                _messagePublisherService.PublishMessage(_edi835GenerationQueueConfigurationSettings, (wrappedMessage));
                _logger.LogInformation("Queue {Type} for {TransactionId}", nameof(DS.Edi835), message.TransactionId);
            }

            await _edi835Repository.Add835Status(message.Edi835Id,
                Edi835StatusCode.QueuedForGeneration,
                 _edi835GenerationQueueConfigurationSettings.QueueName + ":" + _edi835GenerationQueueConfigurationSettings.Host,
                string.Empty,
                 this.GetType().FullName,
                string.Empty,
                cancellationToken);
        }

    }
}
