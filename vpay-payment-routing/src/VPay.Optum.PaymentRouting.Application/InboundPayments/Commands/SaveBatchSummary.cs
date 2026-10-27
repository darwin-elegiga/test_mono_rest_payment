using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;

namespace VPay.Optum.PaymentRouting.Application.InboundPayments.Commands
{
    public static class SaveBatchSummary
    {
        public class Command : IRequest<BatchSummary>
        {
            public PaymentSummary PaymentSummary { get; set; } = new();
        }

        [UsedImplicitly]
        public class Handler : IRequestHandler<Command, BatchSummary>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMediator _mediator;

            public Handler(ILogger<Handler> logger, IMediator mediator)
            {
                _logger = logger;
                _mediator = mediator;
            }

            public async Task<BatchSummary> Handle(Command command, CancellationToken cancellationToken)
            {

                return await _mediator.Send(new InsertInboundBatchSql.Query(
                    command.PaymentSummary.BatchId,
                    command.PaymentSummary.Payer835Id,
                    Convert.ToInt32( command.PaymentSummary.TotalPaymentCount ),
                    Convert.ToDecimal( command.PaymentSummary.TotalPaymentAmount ) )
                    ,cancellationToken).ConfigureAwait(false);
 
            }
        }

    }
}
