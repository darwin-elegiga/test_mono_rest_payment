using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.Models;

namespace VPay.Optum.PaymentRouting.Data.Sql.ApReap.Queries
{
    public static class GetApMessageListByTransactionId
    {
        /// <summary>
        /// Gets the list of AP messages for a given transaction ID and optional AP message type.
        /// </summary>
        /// <param name="TransactionId">The required transaction ID.</param>
        /// <param name="apMessageType">The optional message type: AP or RP. The default is NULL for both types.</param>
        /// <param name="apRoutingType">The optional routing type: Opach, FinSeg, Azure, or NULL for all types.</param>
        /// <returns>The list of AP messages matching the criteria or empty list if none of the records match the criteria.</returns>
        public record Query(long TransactionId, string apMessageType = null, string apRoutingType = null)
            : IRequest<IEnumerable<ApMessage>>;

        public class Handler : IRequestHandler<Query, IEnumerable<ApMessage>>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection)
            {
                _connection = connection;
            }

            public Task<IEnumerable<ApMessage>> Handle(Query request, CancellationToken cancellationToken)
            {
                return _connection.GetAsync<ApMessage>(
                    "dbo.[usp_ApMessage_GetListByTransactionId]",
                    request,
                    cancellationToken
                );
            }
        }
    }
}
