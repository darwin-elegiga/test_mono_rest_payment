using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.SmartEnums;

namespace VPay.Optum.PaymentRouting.Data.Sql.ApReap.Commands
{
    public class UpdateApMessageTrackingStatus
    {
        public class Command : IRequest<Unit>
        {
            public long ApMessageTrackingId { get; set; }
            public ApMessageTrackingStatus Status { get; set; }
            public string ModifiedBy { get; set; }
        }

        public class Handler : IRequestHandler<Command, Unit>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection)
            {
                _connection = connection;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                return await _connection.SaveFirstOrDefaultAsync<Unit>("dbo.usp_ApMessageTrackingStatus_UpdateStatus", request, cancellationToken);
            }
        }
    }
}
