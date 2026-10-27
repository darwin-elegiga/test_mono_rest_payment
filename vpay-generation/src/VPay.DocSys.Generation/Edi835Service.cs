using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Edi835;
using VPay.DocSys.Generation.Models;
using VPay.Queuing;
using static VPay.DocSys.Common.ArgumentHelpers;
using DS = VPay.DocSys.Data;

namespace VPay.DocSys.Generation
{
    public class Edi835Service : IEdi835Service
    {
        private readonly IEdi835Repository _edi835Repository;
        private readonly QueueConfigurations _queueConfigurations;
        private readonly IQueueService _queueService;
        private readonly ILogger<Edi835Service> _logger;

        public Edi835Service(
            IEdi835Repository edi835Repository,
            QueueConfigurations queueConfigurations,
            IQueueService queueService,
            ILogger<IMessagePublisher> loggerMessagePublisher,
            ILogger<Edi835Service> logger)
        {
            ThrowIf.IsChildPropertyNull(queueConfigurations.Edi835GenerationQueue, nameof(queueConfigurations), nameof(queueConfigurations.Edi835GenerationQueue));
            ThrowIf.IsNull(queueService, nameof(queueService));
            ThrowIf.IsNull(loggerMessagePublisher, nameof(loggerMessagePublisher));
            ThrowIf.IsNull(logger, nameof(logger));
            _edi835Repository = edi835Repository;
            _queueConfigurations = queueConfigurations;
            _queueService = queueService;
            _logger = logger;
        }

        public async Task<Dictionary<long, string>> PublishTransactions(IEnumerable<long> transactionList, string user, string programName, CancellationToken cancellationToken)
        {
            var results = new Dictionary<long, string>();
            try
            {
                foreach (long transactionId in transactionList)
                {
                    results[transactionId] = "Looking up Edi835";
                    var edi835 = await _edi835Repository
                        .GetMostRecent835(transactionId, cancellationToken)
                        .ConfigureAwait(false);
                    if (edi835 == null)
                    {
                        results[transactionId] = "Not an Edi835";
                        continue;
                    }
                    if (edi835.CanBeQueuedForGeneration())
                    {
                        await _edi835Repository.Add835Status(edi835.Edi835Id, Edi835StatusCode.QueuedForGeneration,
                            "Post to Endpoint", null, programName, user, cancellationToken)
                            .ConfigureAwait(false);
                        this.QueueMessage(edi835);
                        results[transactionId] = "Message queued & status updated to QueuedForGeneration.";
                    }
                    else
                    {
                        results[transactionId] = $"Edi835 cannot be queued.({edi835.StatusCode}:{edi835.StatusDescription})";
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Data.Add("PartialResults", results);
                _logger.LogError(ex, ex.Message, results);
                throw;
            }
            return results;
        }


        public void QueueMessage(DS.Edi835 edi835)
        {
            ThrowIf.IsNull(edi835, nameof(edi835));
            var messageString = JsonConvert.SerializeObject(new MessageWrapper<DS.Edi835>(edi835));
            _queueService.PublishQueueMessage(_queueConfigurations.Edi835GenerationQueue, messageString);
            _logger.LogInformation("Published Edi835 Generation Message for {TransactionId}", edi835.TransactionId);

        }
    }
}
