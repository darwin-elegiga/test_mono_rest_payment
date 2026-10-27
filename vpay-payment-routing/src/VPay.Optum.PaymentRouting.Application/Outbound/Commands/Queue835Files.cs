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
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Commands;

namespace VPay.Optum.PaymentRouting.Application.Outbound.Commands
{
    public class Queue835Files
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
                _logger.LogInformation("Retrieving payments marked for outbound 835");

                var payments = (await _mediator.Send(new GetInboundPaymentByStatus.Query(PaymentStatus.Out835Built),
                    cancellationToken)).ToList();

                if (payments.Any())
                {
                    var groups = payments.GroupBy(x => new { x.PayerAlias, x.OutboundRefEo });

                    foreach (var group in groups)
                    {
                        var key = group.Key;
                        var paymentIds = group.Select(x => x.InboundPaymentId);
                        var outboundId = await _mediator.Send(
                            new InsertOutbound835File.Command(key.PayerAlias, key.OutboundRefEo, Environment.MachineName, paymentIds),
                            cancellationToken);

                        await _publishEndpoint.Publish(new Outbound835FileMessage { Id = outboundId },
                            cancellationToken);
                        resultCount++;
                    }
                }
                else
                {
                    _logger.LogInformation("Could not find payments marked for outbound 835");
                }

                return resultCount;
            }
        }
    }
}
