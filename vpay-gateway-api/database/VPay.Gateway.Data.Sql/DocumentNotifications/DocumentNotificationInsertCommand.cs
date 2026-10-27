using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.DocumentNotifications;

public class DocumentNotificationInsertCommand : IRequest<DocumentNotification>
{
    public DocumentNotificationInsertCommand(
        long transactionId,
        long archiveDetailId,
        string clientCode,
        string billingEntityCode,
        string? billingEntityType,
        string status,
        string paymentId,
        string documentDescription,
        string documentType,
        string documentName)
    {
        TransactionId = transactionId;
        ArchiveDetailId = archiveDetailId;
        ClientCode = clientCode;
        BillingEntityCode = billingEntityCode;
        BillingEntityType = billingEntityType;
        Status = status;
        PaymentId = paymentId;
        DocumentDescription = documentDescription;
        DocumentName = documentName;
        DocumentType = documentType;
    }

    public long TransactionId { get; }

    public long ArchiveDetailId { get; }

    public string ClientCode { get; }

    public string BillingEntityCode { get; }

    public string? BillingEntityType { get; }

    public string Status { get; }

    public string PaymentId { get; }

    public string DocumentDescription { get; }

    public string DocumentType { get; }

    public string DocumentName { get; }
}

public class DocumentNotificationInsertCommandHandler : IRequestHandler<DocumentNotificationInsertCommand, DocumentNotification>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public DocumentNotificationInsertCommandHandler(IDataConnection<SqlConnection> connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public Task<DocumentNotification> Handle(DocumentNotificationInsertCommand request, CancellationToken cancellationToken) =>
        _connection.SaveFirstOrDefaultAsync<DocumentNotification, SqlConnection>(
            "[dbo].[usp_DocumentNotification_Insert]",
            new
            {
                request.TransactionId,
                request.ArchiveDetailId,
                request.ClientCode,
                request.BillingEntityCode,
                request.BillingEntityType,
                request.Status,
                request.PaymentId,
                request.DocumentDescription,
                request.DocumentType,
                request.DocumentName
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
}
