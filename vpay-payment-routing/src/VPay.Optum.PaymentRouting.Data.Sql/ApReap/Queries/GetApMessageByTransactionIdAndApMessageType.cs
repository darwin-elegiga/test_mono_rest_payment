using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.Models;

namespace VPay.Optum.PaymentRouting.Data.Sql.ApReap.Queries
{
    public static class GetApMessageByTransactionIdAndApMessageType
    {
        /// <summary>
        /// Gets the AP message by transaction ID and AP message type.
        /// </summary>
        /// <param name="TransactionId">The required transaction Id.</param>
        /// <param name="ApMessageType">The required message type: AP or RP.</param>
        /// <returns>The AP message matching the criteria or NULL if no record matches the criteria.</returns>
        public record Query(long TransactionId, string ApMessageType) : IRequest<ApMessage>;

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
                    "dbo.[usp_ApMessage_GetByTransactionIdAndApMessageType]",
                    request,
                    cancellationToken
                );
            }
        }
    }
}
