using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.Helpers;

namespace VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Commands
{
    public class InsertOutbound835File
    {
        public record Command(string PayerAlias, string OutboundRefEo, string CreatedBy,
            IEnumerable<long> paymentIds) : IRequest<long>;

        public class Handler : IRequestHandler<Command, long>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection)
            {
                _connection = connection;
            }

            public async Task<long> Handle(Command request, CancellationToken cancellationToken)
            {
                return await _connection.GetFirstOrDefaultAsync<long>(
                    "dbo.usp_Outbound835File_Insert",
                    new
                    {
                        request.PayerAlias,
                        request.OutboundRefEo,
                        request.CreatedBy,
                        Payments = request.paymentIds.ToList().ToIdDataTable().AsTableValuedParameter("IdTable")
                    },
                    cancellationToken
                );
            }
        }
    }
}
