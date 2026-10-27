using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.Models;

namespace VPay.Optum.PaymentRouting.Data.Sql.ApReap.Queries
{
    public static class GetApMessageById
    {
        /// <summary>
        /// Gets the AP message by the AP message ID unique identifier.
        /// </summary>
        /// <param name="Id">The required Id unique identifier.</param>
        /// <returns>The AP message matching the criteria or NULL if no record matches the criteria.</returns>
        public record Query(long Id) : IRequest<ApMessage>;

        public class Handler : IRequestHandler<Query, ApMessage>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection)
            {
                _connection = connection;
            }

            public Task<ApMessage> Handle(Query request, CancellationToken cancellationToken)
            {
                return _connection.GetFirstOrDefaultAsync<ApMessage>(
                    "dbo.[usp_ApMessage_GetById]",
                    request,
                    cancellationToken
                );
            }
        }
    }
}
