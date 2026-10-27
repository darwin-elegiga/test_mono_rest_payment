using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.Helpers;
using VPay.Optum.PaymentRouting.Data.Sql.Models;

namespace VPay.Optum.PaymentRouting.Data.Sql.ApReap.Queries
{
    public static class GetApMessageListByTransactionIds
    {
        /// <summary>
        /// Gets the list of AP messages for a given list of transaction IDs and optional AP message type.
        /// </summary>
        /// <param name="TransactionIds">The required transaction ID list.</param>
        /// <param name="apMessageType">The optional message type: AP or RP. The default is NULL for both types.</param>
        /// <param name="apRoutingType">The optional routing type: Opach, FinSeg, Azure, or NULL for all types.</param>
        /// <returns>The list of AP messages matching the criteria or empty list if none of the records match the criteria.</returns>
        public record Query(IEnumerable<long> TransactionIds, string ApMessageType = null, string ApRoutingType = null)
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
                    "dbo.[usp_ApMessage_GetListByTransactionIds]",
                    new
                    {
                        TransactionIds = request.TransactionIds.ToIdDataTable().AsTableValuedParameter("IdTable"),
                        ApMessageType = request.ApMessageType,
                        ApRoutingType = request.ApRoutingType
                    },
                    cancellationToken
                );
            }
        }
    }
}
