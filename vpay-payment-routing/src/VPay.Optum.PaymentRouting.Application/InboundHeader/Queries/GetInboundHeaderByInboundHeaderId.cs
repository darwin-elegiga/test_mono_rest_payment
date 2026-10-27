using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Data.Sql.InboundHeader;

namespace VPay.Optum.PaymentRouting.Application.InboundHeader.Queries
{
    public static class GetInboundHeaderByInboundHeaderId
    {
        public class Query : IRequest<Data.Sql.InboundHeader.InboundHeader?>
        {
            public long InboundHeaderId { get; set; }
        }

        public class Handler : IRequestHandler<Query, Data.Sql.InboundHeader.InboundHeader?>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMediator _mediator;

            public Handler(ILogger<Handler> logger, IMediator mediator)
            {
                _logger = logger;
                _mediator = mediator;
            }

            public async Task<Data.Sql.InboundHeader.InboundHeader?> Handle(Query request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Getting InboundHeader {InboundHeaderId}", request.InboundHeaderId);

                var dbQuery = new GetInboundHeaderByIdSql.Query(request.InboundHeaderId);
                var result = await _mediator.Send(dbQuery, cancellationToken);

                return result;
            }
        }
    }
}
