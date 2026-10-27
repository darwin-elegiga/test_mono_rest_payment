using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.PepClaims;

public static class EnrollmentStatusRetryCountUpdate
{
    public record Command(string ClientCode, string MemberId, string ClaimId, int RetryCount) : IRequest<PayeeStatus?>;

    public class Handler : IRequestHandler<Command, PayeeStatus?>
    {
        private readonly IDataConnection<SqlConnection> _connection;

        public Handler(IDataConnection<SqlConnection> connection) =>
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));

        public Task<PayeeStatus?> Handle(Command request, CancellationToken cancellationToken) =>
            _connection.SaveFirstOrDefaultAsync<PayeeStatus?, SqlConnection>(
                "[dbo].[usp_PayeeStatus_UpdateRetryCount]",
                new
                {
                    request.ClientCode,
                    request.MemberId,
                    request.ClaimId,
                    request.RetryCount
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);
    }
}
