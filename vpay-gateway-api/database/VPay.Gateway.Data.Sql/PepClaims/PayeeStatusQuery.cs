using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.PepClaims;

public sealed record PayeeStatusQuery(
    long PayeeId,
    string ClaimId,
    string Status,
    string? PartyStatus
) : IRequest<PayeeStatus?>;

public class PayeeStatusQueryHandler : IRequestHandler<PayeeStatusQuery, PayeeStatus?>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public PayeeStatusQueryHandler(IDataConnection<SqlConnection> connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public Task<PayeeStatus?> Handle(PayeeStatusQuery request, CancellationToken cancellationToken) =>
        _connection.GetFirstOrDefaultAsync<PayeeStatus?, SqlConnection>(
            "[dbo].[usp_PayeeStatus_GetByPayeeIdAndClaimIdAndStatusAndPartyStatus]",
            new
            {
                request.PayeeId,
                request.ClaimId,
                request.Status,
                request.PartyStatus
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
}
