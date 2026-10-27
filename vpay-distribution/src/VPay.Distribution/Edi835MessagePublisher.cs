using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using VPay.Distribution.Common;
using VPay.Distribution.Common.Helpers;
using VPay.Distribution.Common.Models;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using DS = VPay.DocSys.Data;
using VPay.Queuing;

using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.Distribution
{
    public class Edi835MessagePublisher : IEdi835MessagePublisher
    {
        private readonly IEdi835Repository _edi835Repository;
        private readonly IMessagePublisherService _messagePublisherService;
        private readonly Edi835GenerationQueueConfigurationSettings _edi835GenerationQueueConfigurationSettings;
        private readonly ILogger<Edi835MessagePublisher> _logger;
        private const string _nl = "\n"; // Avoid the dev on Windows/ build/test/run on Linux line ending issue.


        public Edi835MessagePublisher(
            IEdi835Repository edi835Repository,
            IMessagePublisherService messagePublisherService,
            Edi835GenerationQueueConfigurationSettings edi835GenerationQueueConfigurationSettings,
            ILogger<Edi835MessagePublisher> logger)
        {

            _edi835Repository = edi835Repository ?? throw new ArgumentNullException(nameof(edi835Repository));
            _messagePublisherService = messagePublisherService ?? throw new ArgumentNullException(nameof(messagePublisherService));
            _edi835GenerationQueueConfigurationSettings = edi835GenerationQueueConfigurationSettings ?? throw new ArgumentNullException(nameof(edi835GenerationQueueConfigurationSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<Edi835PublishResponse> PublishEdi835Messages(DocDistMsg docDistMsg, CancellationToken cancellationToken)
        {
            ThrowIf.IsNull(docDistMsg, nameof(docDistMsg));
            ThrowIf.IsNullOrEmpty(docDistMsg.TPA, nameof(docDistMsg.TPA));
            ThrowIf.IsNullOrEmpty(docDistMsg.BillingEntity, nameof(docDistMsg.BillingEntity));
            ThrowIf.IsNullOrEmpty(docDistMsg.Origin, nameof(docDistMsg.Origin));

            var response = new Edi835PublishResponse();
            var messagesToPublish = new List<DS.Edi835>();
            using (_logger.BeginScope(new
            {
                StatusFileId = docDistMsg.PaymentStatusFileId.ToString().Replace(Environment.NewLine, ""),
                OriginCode = docDistMsg.Origin.Replace(Environment.NewLine, ""),
                BillingEntityCode = docDistMsg.BillingEntity.Replace(Environment.NewLine, ""),
                ClientCode = docDistMsg.TPA.Replace(Environment.NewLine, ""),
                NextWarningTime = docDistMsg.WarningTime.ToString().Replace(Environment.NewLine, ""),
                Transactions = docDistMsg.Transactions?.Count.ToString().Replace(Environment.NewLine, ""),
                IsRedCard = docDistMsg.IsRedCard.ToString().Replace(Environment.NewLine, "")
            }.ToDictionary()))
            {
                if (!(docDistMsg.Transactions?.Any(_ => _.IsEdi835Needed) ?? false))
                {
                    response.Notes = "No transactions for Edi835 generation publishing";
                    _logger.LogInformation(response.Notes);
                    return response;
                }

                response.Edi835TransactionsFound = docDistMsg.Transactions.Where(_ => _.IsEdi835Needed).Select(_ => _.TxnId).ToList();

                var edi835s = (await _edi835Repository.GetMostRecent835s(response.Edi835TransactionsFound, cancellationToken).ConfigureAwait(false)).ToList();

                response.Edi835TransactionsPublished = (await PublishMessages(edi835s, cancellationToken))
                    .Select(e => e.TransactionId).ToList();

            }

            return response;

        }

        private async Task<List<DS.Edi835>> PublishMessages(List<DS.Edi835> messagesToQueue, CancellationToken cancellationToken)
        {
            var messagesPublished = new List<DS.Edi835>();
            foreach (var message in messagesToQueue)
            {
                using (_logger.CreateLogScope(message))
                {
                    var wrappedMessage = JsonConvert.SerializeObject(new MessageWrapper<DS.Edi835>(message));
                    _messagePublisherService.PublishMessage(_edi835GenerationQueueConfigurationSettings, (wrappedMessage));
                    messagesPublished.Add(message);
                    _logger.LogInformation("Queue {Type} for {TransactionId}", nameof(DS.Edi835), message.TransactionId);
                }
            }
            await _edi835Repository.Add835Status(messagesPublished.Select(e => e.Edi835Id),
                Edi835StatusCode.QueuedForGeneration,
                 _edi835GenerationQueueConfigurationSettings.QueueName + ":" + _edi835GenerationQueueConfigurationSettings.Host,
                string.Empty,
                 this.GetType().FullName,
                string.Empty,
                cancellationToken);
            return messagesPublished;
        }
    }
}
