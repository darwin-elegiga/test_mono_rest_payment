using MediatR;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Common.Documents;
using VPay.Gateway.Common.Documents.Models;
using VPay.Gateway.Common.Kubera;
using VPay.Gateway.Common.PlatformConfig;

namespace VPay.Gateway.Application;

public class DownloadDocumentByIdQuery : IRequest<Document?>
{
    public DownloadDocumentByIdQuery(long transactionId, long documentId)
    {
        TransactionId = transactionId;
        DocumentId = documentId;
    }

    public long TransactionId { get; }

    public long DocumentId { get; }
}

public class DownloadDocumentByIdQueryHanlder : IRequestHandler<DownloadDocumentByIdQuery, Document?>
{
    private readonly IMediator _mediator;
    private readonly IDocumentsApi _documentsApi;
    private readonly IUserInfo _userInfo;

    public DownloadDocumentByIdQueryHanlder(IMediator mediator, IDocumentsApi documentsApi, IUserInfo userInfo)
    {
        _mediator = mediator;
        _documentsApi = documentsApi;
        _userInfo = userInfo;
    }

    public async Task<Document?> Handle(DownloadDocumentByIdQuery request, CancellationToken cancellationToken)
    {
        var trans = await _mediator.Send(new GetTransactionByIdQuery(request.TransactionId), cancellationToken).ConfigureAwait(false);
        if (trans == null)
        {
            return null;
        }

        if (!_userInfo.CanAccess(trans.ClientCode, trans.BillingEntityCode, trans.PaymentTypeCode))
        {
            throw new NotAuthorizedException($"Access is denied for the transaction with ID '{request.TransactionId}'");
        }

        var files = await _documentsApi.ListDocuments(request.TransactionId, cancellationToken).ConfigureAwait(false);

        var detail = files.SelectMany(x => x.ArchiveDetails).FirstOrDefault(x => x.ArchiveDetailId == request.DocumentId);
        if (detail == null)
        {
            throw new DocumentNotInArchiveException($"Document (id: {request.DocumentId}) does not belong to the transaction (id: {request.TransactionId})");
        }

        bool isAccessible = false;

        switch (detail.FileDetailType)
        {
            case "Generic":
            case "Pdf":
            case "Limited":
            case "ClearedCheck":
                isAccessible = true;
                break;

            case "FaxDocument":
                var billingEntity = await _mediator.Send(new BillingEntityQuery(trans.ClientCode, trans.BillingEntityCode), cancellationToken).ConfigureAwait(false);
                isAccessible = billingEntity.IsWarranty();
                break;
        }

        if (!isAccessible)
        {
            throw new NotAuthorizedException($"Access is denied for document [{request.TransactionId} - {request.DocumentId}] because of fileType [{detail.FileDetailType}]");
        }

        var result = await _documentsApi.DownloadFile(request.DocumentId, cancellationToken).ConfigureAwait(false);

        var fileName = result.Headers.ContentDisposition?.FileName ?? detail.FileName;
        var mediaType = result.Headers.ContentType?.MediaType ?? (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf" : "text/plain");

        return new Document(
            await result.ReadAsStreamAsync().ConfigureAwait(false),
            mediaType,
            fileName);
    }
}
