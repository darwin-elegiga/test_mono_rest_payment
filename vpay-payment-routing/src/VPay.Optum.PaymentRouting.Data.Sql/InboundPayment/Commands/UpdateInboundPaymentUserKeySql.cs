using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.SmartEnums;

namespace VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Commands
{
    /// <exception cref="SqlException">when [InboundPayment] record not found for <see cref="Command.InboundPaymentId"/></exception>
    /// <exception cref="SqlException">when [InboundPayment] record not found for <see cref="Command.InboundPaymentId"/> and <see cref="Command.UserKey"/></exception>
    /// <exception cref="SqlException">when [InboundPayment].[OutboundRefEo] is not null</exception>
    /// <exception cref="SqlException">when [InboundBatch].[Status] is COMPLETE for [InboundBatch].[Id] = [InboundPayment].[InboundBatchId]</exception>
    public static class UpdateInboundPaymentUserKeySql
    {
        public record Command(long InboundPaymentId, string UserKey, string JiraIssueId)
            : IRequest<Result>;

        public record Result(string UserKey);

        public class Handler : IRequestHandler<Command, Result>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
            {
                await _connection.SaveExecuteAsync(
                    "dbo.[usp_InboundPayment_Update_UserKey]",
                    request,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken
                );

                return new Result(request.UserKey + "_" + request.JiraIssueId);
            }
        }
    }
}
