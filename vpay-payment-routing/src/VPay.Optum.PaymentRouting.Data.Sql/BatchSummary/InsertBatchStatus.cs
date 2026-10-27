using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.BatchSummary
{
    public static class InsertBatchStatus
    {
        public class Command : IRequest<int>
        {
            public long InboundBatchId { get; set; }
            public string Status { get; set; }
        }

        public class Handler : IRequestHandler<Command, int>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(ILogger<Handler> logger, IDataConnection<SqlConnection> connection)
            {
                _logger = logger;
                _connection = connection;
            }
            
            public async Task<int> Handle(Command request, CancellationToken cancellationToken)
            {
                return await _connection.SaveFirstOrDefaultAsync<int>("dbo.[usp_InboundBatchStatus_Update]", request, cancellationToken);
            }
        }
    }
}
