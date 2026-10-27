using MediatR;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Common.Guidewire.OptumAPI;

namespace VPay.Gateway.Common.Guidewire;

public class SendDocumentNoticeCommand : IRequest<GuidewireResult>
{
    public SendDocumentNoticeCommand(string paymentId, long transactionId, long documentId, string description, string documentType, string documentName, GuidewireSettings settings)
    {
        PaymentId = paymentId;
        TransactionId = transactionId;
        DocumentId = documentId;
        Description = description;
        DocumentType = documentType;
        DocumentName = documentName;
        Settings = settings;
    }

    public string PaymentId { get; }

    public long TransactionId { get; }

    public long DocumentId { get; }

    public string Description { get; }

    public string DocumentType { get; }

    public string DocumentName { get; }

    public GuidewireSettings Settings { get; }
}

public class SendDocumentNoticeCommandHandler : IRequestHandler<SendDocumentNoticeCommand, GuidewireResult>
{
    private readonly IGuidewireServiceProxy _guidewireService;

    public SendDocumentNoticeCommandHandler(IGuidewireServiceProxy guidewireService) =>
        _guidewireService = guidewireService;

    public async Task<GuidewireResult> Handle(SendDocumentNoticeCommand request, CancellationToken cancellationToken)
    {
        var document = new OptumNotifyDocumentDetails
        {
            PaymentID = request.PaymentId,
            DocumentDetails = new[]
            {
                new OptumDocumentDetails
                {
                    DocumentDescription = request.Description,
                    DocumentID = request.DocumentId.ToString(),
                    TransactionID = request.TransactionId.ToString(),
                    DocumentType = request.DocumentType,
                    DocumentName = request.DocumentName
                }
            }
        };

        return await _guidewireService.NotifyDocumentDetails(document, request.Settings).ConfigureAwait(false);
    }
}
