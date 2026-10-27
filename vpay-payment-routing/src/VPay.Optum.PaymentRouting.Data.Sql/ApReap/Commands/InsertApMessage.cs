using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.ApReap.Commands
{
    public class InsertApMessage
    {
        public class Command : IRequest<Result<long>>
        {
            public long TransactionId { get; set; }
            public string ClientCode { get; set; } = string.Empty;
            public string BillingEntityCode { get; set; } = string.Empty;
            public string ApMessageType { get; set; } = string.Empty;
            public string ApRoutingType { get; set; } = string.Empty;
            public bool IsOptumEnrolled { get; set; }
            // Avro Fields
            public string PayerId { get; set; } = string.Empty;
            public string SubPayerId { get; set; } = string.Empty;
            public string PaymentSource { get; set; } = string.Empty;
            public string PaymentType { get; set; } = string.Empty;
            public string ProviderNumber { get; set; } = string.Empty;
            public string ProviderName { get; set; } = string.Empty;
            public string ProviderNpiNumber { get; set; } = string.Empty;
            public string UnConsolPayNumber { get; set; } = string.Empty;
            public string UnConsolPayAmount { get; set; } = string.Empty;
            public string DisplayConsolPayNumber { get; set; } = string.Empty;
            public string ConsolAmount { get; set; } = string.Empty;
            public string PayMethodCode { get; set; } = string.Empty;
            public string SettlementDate { get; set; } = string.Empty;
            public string CheckNumber { get; set; } = string.Empty;
            public string ReturnReasonCode { get; set; } = string.Empty;
            public string RevisedMethodCode { get; set; } = string.Empty;
            public string ReOriginatedDate { get; set; } = string.Empty;
            // Auditing fields
            public string CreatedBy { get; set; } = string.Empty;
        }

        [UsedImplicitly]
        public class Handler : IRequestHandler<Command, Result<long>>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection)
            {
                _connection = connection;
            }

            public async Task<Result<long>> Handle(Command request, CancellationToken cancellationToken)
            {
                try
                {
                    long newId = await _connection.SaveFirstOrDefaultAsync<long>("dbo.usp_ApMessage_Insert", request, cancellationToken);
                    return Result<long>.Success(newId);
                }
                catch (SqlException sqlEx)
                {
                    if (sqlEx.Number == SqlErrorCode.UNIQUE_INDEX_VIOLATION || sqlEx.Number == SqlErrorCode.DUPLICATE_KEY_VIOLATION)
                    {
                        // Allow consumer to handle duplicate records.
                        return Result<long>.Error(sqlEx.Message);
                    }

                    // Re-throw to send message to error queue. 
                    throw;
                }
            }
        }
    }
}
