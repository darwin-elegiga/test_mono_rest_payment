using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.BatchManifest;

namespace VPay.Optum.PaymentRouting.Application.InboundPayments.Queries
{
    public static class GetBatchWithPayments
    {

        public record Query : IRequest<Result>
        {
            public long Id { get; set; }
        }

        public record Result
        {
            public long InboundBatchId { get; init; }
            public long InboundHeaderId { get; init; }
            public string OptumBatchId { get; init; } = string.Empty;
            public string Payer835Id { get; init; } = string.Empty;
            public string PayerSubpayerId { get; init; } = string.Empty;
            public string PayerAlias { get; init; } = string.Empty;
            public int TotalPaymentCount { get; init; }
            public decimal TotalPaymentAmount { get; init; }
            public DateTime BatchCreatedDateTime { get; init; }
            public DateTime HeaderCreatedDateTime { get; init; }
            public string HeaderData { get; init; } = string.Empty;
            public BatchStatus Status { get; init; } = BatchStatus.BatchInitiated;
            public IEnumerable<long> InboundPaymentIds { get; set; } = Array.Empty<long>();
        }

        [UsedImplicitly]
        public class Handler : IRequestHandler<Query, Result?>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMediator _mediator;
            
            public Handler(IMediator mediator, ILogger<Handler> logger)
            {
                _mediator = mediator;
                _logger = logger;
            }

            public async Task<Result?> Handle(Query query, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Fetching inbound batch with id: {batchId}", query.Id);
                BatchManifest batch = await _mediator.Send(new GetBatchManifestSql.Query(query.Id), cancellationToken);
                _logger.LogInformation("Retrieved inbound batch with id: {batchId}", query.Id);
                return batch == null ? null : new Result
                {
                    InboundBatchId = batch.InboundBatchId,
                    Payer835Id = batch.Payer835Id,
                    PayerAlias = batch.PayerAlias,
                    PayerSubpayerId = batch.PayerSubpayerId,
                    HeaderData = batch.HeaderData,
                    BatchCreatedDateTime = batch.BatchCreatedDateTime,
                    HeaderCreatedDateTime = batch.HeaderCreatedDateTime,
                    InboundHeaderId = batch.InboundHeaderId,
                    InboundPaymentIds = batch.InboundPaymentIds,
                    OptumBatchId = batch.OptumBatchId,
                    TotalPaymentAmount = batch.TotalPaymentAmount,
                    TotalPaymentCount = batch.TotalPaymentCount,
                    Status = batch.Status
                };
            }
        }

    }
}
