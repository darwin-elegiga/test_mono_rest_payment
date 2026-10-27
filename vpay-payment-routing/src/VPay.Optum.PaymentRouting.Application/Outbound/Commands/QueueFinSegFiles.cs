using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.OutboundFinSeg.Commands;

namespace VPay.Optum.PaymentRouting.Application.Outbound.Commands
{

    /// <summary>
    /// This if very similar to the Queue835Files command,
    /// except, this is looking for InboundPayment by Status FinSegBuilt
    /// </summary>
    public class QueueFinSegFiles
    {
        public record Command : IRequest<int>;

        public class Handler : IRequestHandler<Command, int>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMediator _mediator;
            private readonly IPublishEndpoint _publishEndpoint;

            public Handler(ILogger<Handler> logger, IMediator mediator, IPublishEndpoint publishEndpoint)
            {
                _logger = logger;
                _mediator = mediator;
                _publishEndpoint = publishEndpoint;
            }

            public async Task<int> Handle(Command request, CancellationToken cancellationToken)
            {
                var resultCount = 0;
                _logger.LogInformation("Retrieving payments marked for FINSEG/835");

                var payments = (await _mediator.Send(new GetInboundPaymentByStatus.Query(PaymentStatus.FinSegBuilt),
                    cancellationToken)).ToList();

                if (payments.Any())
                {
                    var groups = payments.GroupBy(x => new { x.PayerAlias, x.OutboundRefEo });

                    foreach (var group in groups)
                    {
                        var key = group.Key;
                        var paymentIds = group.Select(x => x.InboundPaymentId);
                        var dateStamp = DateTime.Now.ToString("yyyyMMdd");
                        var outboundFinSegSequenceId = await _mediator.Send(
                            new GetNextFinSegSequence.Command(key.PayerAlias, dateStamp, Environment.MachineName),
                        cancellationToken).ConfigureAwait(false);


                        var finSegIds = await _mediator.Send(
                            new InsertOutboundFinSegFiles.Command(
                                outboundFinSegSequenceId.OutboundFinSegSequenceId,
                                key.PayerAlias,
                                key.OutboundRefEo,
                                Environment.MachineName,
                                paymentIds),cancellationToken).ConfigureAwait(false);

                        await _publishEndpoint.Publish(new OutboundFinSegFileMessage
                        {
                            OutboundFinSeg835FileId = finSegIds.OutboundFinSeg835FileId,
                            OutboundFinSegTxtFileId = finSegIds.OutboundFinSegTxtFileId
                        }, cancellationToken).ConfigureAwait(false);
                        resultCount++;
                    }
                }
                else
                {
                    _logger.LogInformation("Could not find payments marked for FINSEG/835");
                }

                return resultCount;
            }
        }
    }
}
