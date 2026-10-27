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
    public static class SavePaymentRecord
    {
        public class Command : IRequest<BatchSummary>
        {
            public PaymentRecord PaymentRecord { get; set; } = new();
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
                if ((command?.PaymentRecord?.TransactionType ?? "").Equals("EOF", StringComparison.OrdinalIgnoreCase))
                {
                   return await _mediator.Send(new InsertInboundHeaderSql.Command(
                        command.PaymentRecord.BatchId,
                        command.PaymentRecord.Payer835Id,
                        command.PaymentRecord.PayerSubpayerId,
                        command.PaymentRecord.FileProcId,
                        command.PaymentRecord.PayerAlias,
                        command.PaymentRecord.PaymentClob
                        ), cancellationToken).ConfigureAwait(false);
                }
                else
                if ((command?.PaymentRecord?.TransactionType ?? "").Equals("835", StringComparison.OrdinalIgnoreCase))
                {
                    return await _mediator.Send(new InsertInboundPaymentSql.Command(
                        command.PaymentRecord.BatchId,
                        command.PaymentRecord.Payer835Id,
                        command.PaymentRecord.PayerSubpayerId,
                        command.PaymentRecord.FileProcId,
                        command.PaymentRecord.PayerAlias,
                        command.PaymentRecord.ProviderTin,
                        command.PaymentRecord.ZipCode,
                        command.PaymentRecord.UnconsolidatedPaymentNbr,
                        Convert.ToDecimal( command.PaymentRecord.UnconsolidatedPaymentAmount ),
                        command.PaymentRecord.PaymentClob,
                        command.PaymentRecord.RemitMethod

                    ), cancellationToken).ConfigureAwait(false);

                }  

                throw new InvalidOperationException();

            }
        }
    }
}
