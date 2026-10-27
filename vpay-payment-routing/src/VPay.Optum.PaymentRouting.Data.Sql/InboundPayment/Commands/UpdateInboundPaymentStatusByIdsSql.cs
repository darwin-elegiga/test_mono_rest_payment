using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.Helpers;

namespace VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Commands
{
    public class UpdateInboundPaymentStatusByIdsSql
    {
        public class Command : IRequest<Unit>
        {
            public IEnumerable<long> InboundPaymentIds { get; set; }
            public PaymentStatus Status { get; set; }
            public string ModifiedBy { get; set; }
        }

        public class Handler : IRequestHandler<Command, Unit>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                await _connection.SaveExecuteAsync("dbo.[usp_InboundPayment_Update_Status_ByIds]",
                        new
                        {
                            InboundPaymentIds = request.InboundPaymentIds.ToIdDataTable().AsTableValuedParameter("IdTable"),
                            request.Status,
                            request.ModifiedBy
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
                return Unit.Value;
            }
        }
    }
}
