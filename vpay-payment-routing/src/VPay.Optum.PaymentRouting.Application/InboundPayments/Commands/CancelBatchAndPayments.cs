using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;
using VPay.Optum.PaymentRouting.Data.Sql.InboundBatch.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;

namespace VPay.Optum.PaymentRouting.Application.InboundPayments.Commands
{
    /// <exception cref="InvalidOperationException">when [InboundBatch] record not found for <see cref="InboundBatchId"/></exception>
    /// <exception cref="InvalidOperationException">when [InboundBatch].[Status] is BatchComplete or BatchCanceled</exception>
    /// <exception cref="InvalidOperationException">when [InboundPayments].[UserKey] ends with the Jira Ticket Number</exception>
    public static class CancelBatchAndPayments
    {
        public record Command(long InboundBatchId, string JiraTicket) : IRequest;

        [UsedImplicitly]
        public class Handler : IRequestHandler<Command>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMediator _mediator;

            public Handler(ILogger<Handler> logger, IMediator mediator)
            {
                _logger = logger;
                _mediator = mediator;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Marking batch and payments as canceled with id: {batchId}", request.InboundBatchId);

                await EnsureValidity(request.InboundBatchId, request.JiraTicket, cancellationToken);

                await _mediator.Send(new CancelBatchAndPaymentsSql
                    .Command(request.InboundBatchId, request.JiraTicket), cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation("Marked batch and payments canceled with id: {batchId}", request.InboundBatchId);

                return Unit.Value;
            }

            private async Task EnsureValidity(long inboundBatchId, string jiraTicket, CancellationToken cancellationToken)
            {
                var batch = await _mediator.Send(new GetBatchSummarySql
                    .Query(inboundBatchId), cancellationToken);

                if (batch == null)
                {
                    throw new InvalidOperationException($"No [InboundBatch] record exists for [Id] = {inboundBatchId}");
                }

                if (batch.Status == BatchStatus.BatchComplete)
                {
                    throw new InvalidOperationException($"Cannot update [InboundBatch].[Status] when [Status] = {BatchStatus.BatchComplete.Name}");
                }

                if (batch.Status == BatchStatus.BatchCancelled)
                {
                    throw new InvalidOperationException($"Cannot update [InboundBatch].[Status] when [Status] = {BatchStatus.BatchCancelled.Name}");
                }

                var payments = await _mediator.Send(
                    new GetInboundPaymentsWithoutClobByBatchIdSql.Query(batch.InboundBatchId), cancellationToken);

                var badPayments = payments.Where(p =>
                        p.UserKey.EndsWith(jiraTicket, StringComparison.CurrentCultureIgnoreCase));

                if (badPayments.Any())
                {
                    throw new InvalidOperationException($"Cannot update [InboundPayments] when they end with a Jira ticket number for [InboundBatchId] = {inboundBatchId}");
                }
            }
        }
    }
}
