using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;

namespace VPay.Optum.PaymentRouting.Application.InboundPayments.Queries
{
    public static class GetPaymentRecord
    {
        public class Query : IRequest<Result>
        {
            public long Id { get; set; }
        }

        public record Result
        {
            public long Id { get; init; }

            public long InboundBatchId { get; init; }

            public string PaymentClob { get; init; } = string.Empty;

            public string UserKey { get; init; } = string.Empty;

            public string Payer835Id { get; init; } = string.Empty;

            public string FileProcId { get; init; } = string.Empty;

            public string PayerAlias { get; init; } = string.Empty;

            public string ProvTin { get; init; } = string.Empty;

            public string PayerSubpayerId { get; init; } = string.Empty;

            public string ZipCode { get; init; } = string.Empty;

            public decimal PaymentAmount { get; init; }

            public DateTime CreateDateTime { get; init; }

            public string RemitMethod { get; set; } = default!;
        }

        [UsedImplicitly]
        public class Handler : IRequestHandler<Query, Result?>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMediator _mediator;

            public Handler(ILogger<Handler> logger, IMediator mediator)
            {
                _logger = logger;
                _mediator = mediator;
            }

            public async Task<Result?> Handle(Query query, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Fetching inbound payment with id: {inboundPaymentId}", query.Id);
                GetInboundPaymentWithClobByIdSql.Result getInboundPaymentWithClobSqlResult = await _mediator.Send(new GetInboundPaymentWithClobByIdSql.Query {InboundPaymentId = query.Id}, cancellationToken);
                _logger.LogInformation("Retrieved inbound payment with id: {inboundPaymentId}", query.Id);
                return getInboundPaymentWithClobSqlResult == null ? null : new Result
                {
                    Id = getInboundPaymentWithClobSqlResult.Id,
                    Payer835Id = getInboundPaymentWithClobSqlResult.Payer835Id,
                    FileProcId = getInboundPaymentWithClobSqlResult.FileProcId,
                    PayerAlias = getInboundPaymentWithClobSqlResult.PayerAlias,
                    CreateDateTime = getInboundPaymentWithClobSqlResult.CreateDateTime,
                    InboundBatchId = getInboundPaymentWithClobSqlResult.InboundBatchId,
                    ZipCode = getInboundPaymentWithClobSqlResult.ZipCode,
                    PayerSubpayerId = getInboundPaymentWithClobSqlResult.PayerSubpayerId,
                    PaymentAmount = getInboundPaymentWithClobSqlResult.PaymentAmount,
                    PaymentClob = getInboundPaymentWithClobSqlResult.PaymentClob,
                    ProvTin = getInboundPaymentWithClobSqlResult.ProvTIN,
                    UserKey = getInboundPaymentWithClobSqlResult.UserKey,
                    RemitMethod = getInboundPaymentWithClobSqlResult.RemitMethod
                };
            }
        }
    }
}
