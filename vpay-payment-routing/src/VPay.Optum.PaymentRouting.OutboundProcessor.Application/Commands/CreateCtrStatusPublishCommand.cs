using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Common.Constants;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Queries;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.Application.Commands
{
    /// <summary>
    /// Takes the Outbound835FileId for the 835 file, and puts a message for each
    /// transaction onto a queue for the iSeries to pick and update the status to
    /// SENTOPTUM.
    /// </summary>
    public static class CreateCtrStatusPublishCommand
    {
        public record Command(long Outbound835FileId) : IRequest<int>;

        public class Handler : IRequestHandler<Command, int>
        {
            private readonly ILogger<Handler> _logger;

            //Need the bus because IPublishEndpoint ends up on the Nacha Bus
            private readonly IBus _bus;

            private readonly IMediator _mediator;
            public Handler(ILogger<Handler> logger, IOptumWrapBus bus, IMediator mediator)
            {
                _logger = logger;
                _bus = bus;
                _mediator = mediator;
            }
            public async Task<int> Handle(Command request, CancellationToken cancellationToken)
            {
                var transactions = (await _mediator.Send(
                    new GetOutbound835FileTransactionIdsByOutbound835FileId.Query(request.Outbound835FileId), cancellationToken).ConfigureAwait(false)).ToList();

                if (transactions == null || (transactions.Count  == 0)) // SonarQube didn't understand the null coalescing operator
                {
                    using var scope = _logger.BeginScope(new Dictionary<string, object>
                    {
                        ["Outbound835FileId"] = request.Outbound835FileId
                    });
                    _logger.LogInformation("CreateCtrStatusPublish: Unable to find Outbound835FileId to publish to CreateCtrStatus");
                    return 0;
                }

                int count = 0;
                foreach (var txn in transactions)
                {
                    using var scope = _logger.BeginScope(new Dictionary<string, object>
                    {
                        ["TransactionId"] = txn.TransactionId,
                        ["InboundPaymentId"] = txn.InboundPaymentId,
                        ["OutboundRefEO"] = txn.OutboundRefEO,
                        ["Outbound835FileId"] = txn.Outbound835FileId,
                        ["Outbound835FilePath"] = txn.FilePath,
                        ["Outbound835FileStatus"] = txn.Outbound835FileStatus,
                        ["Outbound835FileSentDateTime"] = txn.Outbound835FileSentDateTime.HasValue ? txn.Outbound835FileSentDateTime.Value.ToString("F") : "NULL",
                        ["Outbound835FileCreatedDateTime"] = txn.Outbound835FileCreatedDateTime,
                        ["Outbound835FileLastModifiedDateTime"] = txn.Outbound835FileLastModifiedDateTime
                    });

                    if (txn.Outbound835FileStatus != Outbound835FileStatus.Completed)
                    {
                        _logger.LogInformation("CreateCtrStatusPublish: Unable to publish transaction, not in Completed status");
                        continue;
                    }

                    var sentDateTime = txn.Outbound835FileSentDateTime.HasValue ? txn.Outbound835FileSentDateTime.Value : txn.Outbound835FileCreatedDateTime;

                    await _bus.Publish(new CreateCtrStatus(txn.TransactionId, CtrStatus.SentOptum, sentDateTime), cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("CreateCtrStatusPublish: Published to queue");
                    count++;
                }
                return count;
            }
        }
    }
}
