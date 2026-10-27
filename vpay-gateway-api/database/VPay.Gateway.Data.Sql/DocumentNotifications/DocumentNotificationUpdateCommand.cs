using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.DocumentNotifications;

public class DocumentNotificationUpdateCommand : IRequest<DocumentNotification?>
{
    public DocumentNotificationUpdateCommand(long transactionId, long archiveDetailId, string status, string previousStatus)
    {
        TransactionId = transactionId;
        ArchiveDetailId = archiveDetailId;
        Status = status;
        PreviousStatus = previousStatus;
    }

    public long TransactionId { get; }

    public long ArchiveDetailId { get; }

    public string Status { get; }

    public string PreviousStatus { get; }
}

public class DocumentNotificationUpdateCommandHandler : IRequestHandler<DocumentNotificationUpdateCommand, DocumentNotification?>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public DocumentNotificationUpdateCommandHandler(IDataConnection<SqlConnection> connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public Task<DocumentNotification?> Handle(DocumentNotificationUpdateCommand request, CancellationToken cancellationToken) =>
        _connection.SaveFirstOrDefaultAsync<DocumentNotification?, SqlConnection>(
            "[dbo].[usp_DocumentNotification_Update]",
            new
            {
                request.TransactionId,
                request.ArchiveDetailId,
                request.Status,
                request.PreviousStatus
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
}
