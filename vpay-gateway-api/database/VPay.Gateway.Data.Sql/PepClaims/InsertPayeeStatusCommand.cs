using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.PepClaims;

public sealed record InsertPayeeStatusCommand(long PayeeId,
    string ClaimId,
    long PepPayeeId,
    string Status,
    string CurrentProcessStatus,
    long PepStatusId,
    DateTime StatusDateTime,
    long? PartyStatusId,
    string? PartyStatus,
    int? RetryCount = 0
) : IRequest<PayeeStatus>;

public class InsertPayeeStatusCommandHandler : IRequestHandler<InsertPayeeStatusCommand, PayeeStatus>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public InsertPayeeStatusCommandHandler(IDataConnection<SqlConnection> connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public Task<PayeeStatus> Handle(InsertPayeeStatusCommand request, CancellationToken cancellationToken) =>
        _connection.SaveFirstOrDefaultAsync<PayeeStatus, SqlConnection>(
            "[dbo].[usp_PayeeStatus_Insert]",
            new
            {
                request.PayeeId,
                request.ClaimId,
                request.PepPayeeId,
                request.Status,
                request.CurrentProcessStatus,
                request.PepStatusId,
                request.StatusDateTime,
                request.PartyStatusId,
                request.PartyStatus,
                request.RetryCount
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
}
