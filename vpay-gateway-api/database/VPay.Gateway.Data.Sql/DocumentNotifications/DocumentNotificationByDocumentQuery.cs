using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.DocumentNotifications;

public class DocumentNotificationByDocumentQuery : IRequest<DocumentNotification>
{
    public DocumentNotificationByDocumentQuery(long transactionId, long archiveDetailId)
    {
        TransactionId = transactionId;
        ArchiveDetailId = archiveDetailId;
    }

    public long TransactionId { get; }

    public long ArchiveDetailId { get; }
}

public class DocumentNotificationByDocumentQueryHandler : IRequestHandler<DocumentNotificationByDocumentQuery, DocumentNotification>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public DocumentNotificationByDocumentQueryHandler(IDataConnection<SqlConnection> connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public Task<DocumentNotification> Handle(DocumentNotificationByDocumentQuery request, CancellationToken cancellationToken) =>
        _connection.GetFirstOrDefaultAsync<DocumentNotification, SqlConnection>(
            "[dbo].[usp_DocumentNotification_GetByDocument]",
            new
            {
                request.TransactionId,
                request.ArchiveDetailId
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
}
