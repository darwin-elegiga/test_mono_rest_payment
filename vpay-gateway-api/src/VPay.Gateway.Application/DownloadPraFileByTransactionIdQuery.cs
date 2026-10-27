using MediatR;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Common.Documents;
using VPay.Gateway.Common.Documents.Models;
using VPay.Gateway.Common.Kubera;

namespace VPay.Gateway.Application;

public record DownloadPraFileByTransactionIdQuery(long TransactionId, bool Masked) : IRequest<Document?>;

public class DownloadPraFileByTransactionIdQueryHandler : IRequestHandler<DownloadPraFileByTransactionIdQuery, Document?>
{
    private readonly IMediator _mediator;
    private readonly IDocumentsApi _documentsApi;
    private readonly IUserInfo _userInfo;

    public DownloadPraFileByTransactionIdQueryHandler(IMediator mediator, IDocumentsApi documentsApi, IUserInfo userInfo)
    {
        _mediator = mediator;
        _documentsApi = documentsApi;
        _userInfo = userInfo;
    }

    public async Task<Document?> Handle(DownloadPraFileByTransactionIdQuery request, CancellationToken cancellationToken)
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

        var result = request.Masked
            ? await _documentsApi.DownloadMaskedDistributionPacket(request.TransactionId, trans.PaymentTypeCode, cancellationToken).ConfigureAwait(false)
            : await _documentsApi.DownloadDistributionPacket(request.TransactionId, trans.PaymentTypeCode, cancellationToken).ConfigureAwait(false);

        if (result.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new ItemNotFoundException(DocumentPraStatuses.GetNotReadyString(trans.DateLoaded));
        }

        await result.EnsureSuccessStatusCodeAsync().ConfigureAwait(false);

        var fileName = result.Content!.Headers.ContentDisposition?.FileName ?? $"pra-{request.TransactionId}.pdf";
        var mediaType = result.Content.Headers.ContentType?.MediaType ?? (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf" : "text/plain");

        var stream = await result.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        return new Document(
            stream,
            mediaType,
            fileName);
    }
}
