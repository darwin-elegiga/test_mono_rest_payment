using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace VPay.Optum.PaymentRouting.Application.Testing.Queries
{
    public class TestCqrs
    {
        public class Query : IRequest
        {
            public int Test { get; set; }
        }

        public class Handler : IRequestHandler<Query, Unit>
        {
            private readonly ILogger<Handler> _logger;

            public Handler(ILogger<Handler> logger)
            {
                _logger = logger;
            }

            public Task<Unit> Handle(Query request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Test query hit!" + request.Test);
                return Unit.Task;
            }
        }
    }
}
