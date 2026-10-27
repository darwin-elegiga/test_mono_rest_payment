using System;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.SmartEnums;

namespace VPay.Optum.PaymentRouting.Data.Sql.ApReap.Commands
{
    public class InsertApMessageTracking
    {
        public class Command : IRequest<Result<long>>
        {
            public long ApMessageId { get; set; }
            public long InboundPaymentId { get; set; }
            public long TransactionId { get; set; }
            public string TrnSegment { get; set; }
            public decimal LoadAmount { get; set; }
            public string PaymentTypeCode { get; set; }
            public string CheckNumber { get; set; }
            public DateTime LoadDate { get; set; }
            public ApMessageTrackingStatus Status { get; set; }
            public string CreatedBy { get; set; }
        }

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
                    long newId = await _connection.SaveFirstOrDefaultAsync<long>("dbo.usp_ApMessageTracking_Insert", request, cancellationToken);
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
