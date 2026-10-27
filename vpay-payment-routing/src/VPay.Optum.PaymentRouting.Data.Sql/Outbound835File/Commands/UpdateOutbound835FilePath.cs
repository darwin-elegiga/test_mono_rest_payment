using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Commands
{
    public class UpdateOutbound835FilePath
    {
        public class Command : IRequest
        {
            public long Outbound835FileId { get; set; }
            public string FilePath { get; set; }
            public string ModifiedBy { get; set; }
        }

        public class Handler : IRequestHandler<Command>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                await _connection.SaveExecuteAsync("dbo.[usp_Outbound835File_Update_FilePath]",
                    request,
                    cancellationToken
                ).ConfigureAwait(false);
                return Unit.Value;
            }
        }
    }
}
