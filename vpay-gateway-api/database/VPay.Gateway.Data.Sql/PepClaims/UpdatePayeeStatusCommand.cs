using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.PepClaims;

public class UpdatePayeeStatusCommand : IRequest<PayeeStatus?>
{
    public UpdatePayeeStatusCommand(long payeeStatusId, string status, string previousStatus)
    {
        PayeeStatusId = payeeStatusId;
        Status = status;
        PreviousStatus = previousStatus;
    }

    public long PayeeStatusId { get; }

    public string Status { get; }

    public string PreviousStatus { get; }
}

public class UpdatePayeeStatusCommandHandler : IRequestHandler<UpdatePayeeStatusCommand, PayeeStatus?>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public UpdatePayeeStatusCommandHandler(IDataConnection<SqlConnection> connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public Task<PayeeStatus?> Handle(UpdatePayeeStatusCommand request, CancellationToken cancellationToken) =>
        _connection.SaveFirstOrDefaultAsync<PayeeStatus?, SqlConnection>(
            "[dbo].[usp_PayeeStatus_Update]",
            new
            {
                request.PayeeStatusId,
                request.Status,
                request.PreviousStatus
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
}
