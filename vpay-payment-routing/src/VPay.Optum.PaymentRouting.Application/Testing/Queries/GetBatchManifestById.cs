using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Data.Sql.BatchManifest;

 

namespace VPay.Optum.PaymentRouting.Application.Testing.Queries
{
    public class GetBatchManifestById
    {
        public record Query(long InboundBatchId) : IRequest<BatchManifest>;

        public class Handler : IRequestHandler<Query, BatchManifest?>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMediator _mediator;

            public Handler(ILogger<Handler> logger, IMediator mediator)
            {
                _logger = logger;
                _mediator = mediator;
            }

            public async Task<BatchManifest?> Handle(Query request, CancellationToken cancellationToken)
            {
                return await _mediator.Send(new GetBatchManifestSql.Query(request.InboundBatchId), cancellationToken);
            }
        }


    }
}
