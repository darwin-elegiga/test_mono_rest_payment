using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Common.Helpers;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.BatchManifest;
using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;

namespace VPay.Optum.PaymentRouting.Application.InboundPayments.Commands
{
    /// <exception cref="InvalidOperationException">when [InboundPayment] record not found for <see cref="InboundPaymentId"/></exception>
    /// <exception cref="InvalidOperationException">when [InboundBatch] record not found for [InboundPayment].[InboundBatchId]</exception>
    /// <exception cref="InvalidOperationException">when [InboundBatch].[Status] is COMPLETE</exception>
    public static class CancelInboundPayment
    {
        public record Command(long InboundPaymentId) : IRequest;

        [UsedImplicitly]
        public class Handler : IRequestHandler<Command>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMediator _mediator;
            private readonly IUserInfo _userInfo;

            public Handler(ILogger<Handler> logger, IMediator mediator, IUserInfo userInfo)
            {
                _logger = logger;
                _mediator = mediator;
                _userInfo = userInfo;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Marking payment cancelled with id: {paymentId}", request.InboundPaymentId);

                await EnsureValidity(request.InboundPaymentId, cancellationToken);

                await _mediator.Send(new UpdateInboundPaymentStatusSql.Command
                {
                    InboundPaymentId = request.InboundPaymentId,
                    Status = PaymentStatus.Cancelled,
                    ModifiedBy = _userInfo.GetCurrentUserName(),
                }, cancellationToken);

                _logger.LogInformation("Marked payment cancelled with id: {paymentId}", request.InboundPaymentId);

                return Unit.Value;
            }

            private async Task EnsureValidity(long inboundPaymentId, CancellationToken cancellationToken)
            {
                var payment = await _mediator.Send(new GetInboundPaymentWithClobByIdSql.Query
                { InboundPaymentId = inboundPaymentId }, cancellationToken);

                if (payment == null)
                {
                    throw new InvalidOperationException($"[InboundPayment] record does not exist for [InboundPayment].[Id] = @InboundPaymentId ({inboundPaymentId})");
                }

                var batch = await _mediator.Send(new GetBatchSummarySql
                    .Query(payment.InboundBatchId), cancellationToken);

                if (batch == null)
                {
                    throw new InvalidOperationException($"[InboundBatch] record does not exist for [InboundBatch].[Id] = [InboundPayment].[InboundBatchId] ({payment.InboundBatchId})");
                }

                if (batch?.Status == BatchStatus.BatchComplete)
                {
                    throw new InvalidOperationException($"[InboundPayment].[Status] update forbidden when [InboundBatch].[Status] = {BatchStatus.BatchComplete.Name}");
                }
            }
        }
    }
}
