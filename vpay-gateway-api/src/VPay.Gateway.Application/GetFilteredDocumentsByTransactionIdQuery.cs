using MediatR;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Common.Documents;
using VPay.Gateway.Common.Documents.Models;
using VPay.Gateway.Common.Kubera;
using VPay.Gateway.Common.Kubera.Models;

namespace VPay.Gateway.Application;
public class GetFilteredDocumentsByTransactionIdQuery : IRequest<IReadOnlyCollection<ArchiveDetail>?>
{
    public GetFilteredDocumentsByTransactionIdQuery(long transactionId)
    {
        TransactionId = transactionId;
    }

    public long TransactionId { get; }
}

/// <summary>
/// Retrieves documents associated with a specific transaction ID, filtered by user access.
/// </summary>
/// <returns>The list of filtered documents when transaction ID exists or null if Transaction ID does not exist. </returns>
/// <exception cref="NotAuthorizedException">When transaction ID does not belong to the requesting user.</exception>
public class GetFilteredDocumentsByTransactionIdQueryHandler : IRequestHandler<GetFilteredDocumentsByTransactionIdQuery, IReadOnlyCollection<ArchiveDetail>?>
{
    private readonly IMediator _mediator;
    private readonly IDocumentsApi _documentsApi;
    private readonly IUserInfo _userInfo;

    public GetFilteredDocumentsByTransactionIdQueryHandler(IMediator mediator, IDocumentsApi documentsApi, IUserInfo userInfo)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _documentsApi = documentsApi ?? throw new ArgumentNullException(nameof(documentsApi));
        _userInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
    }

    /// <summary>
    /// Retrieves documents associated with a specific transaction ID, filtered by user access.
    /// </summary>
    /// <param name="request">The requested transaction Id.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The list of filtered documents when transaction ID exists or null if Transaction ID does not exist. </returns>
    /// <exception cref="NotAuthorizedException">When transaction ID does not belong to the requesting user.</exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public async Task<IReadOnlyCollection<ArchiveDetail>?> Handle(GetFilteredDocumentsByTransactionIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.TransactionId <= 0)
        {
            return null;
        }

        Transaction? transaction = await _mediator.Send(new GetTransactionByIdQuery(request.TransactionId), cancellationToken).ConfigureAwait(false);

        if (transaction == null)
        {
            return null;
        }

        if (!_userInfo.CanAccess(transaction.ClientCode, transaction.BillingEntityCode, transaction.PaymentTypeCode))
        {
            throw new NotAuthorizedException($"Access is denied for the transaction with ID '{request.TransactionId}'");
        }

        var files = await _documentsApi.ListDocuments(request.TransactionId, cancellationToken).ConfigureAwait(false);

        IEnumerable<ArchiveDetail> archiveFiles = files.SelectMany(x => x.ArchiveDetails ?? Enumerable.Empty<ArchiveDetail>());
        List<ArchiveDetail> results = [];

        foreach (var detail in archiveFiles)
        {
            detail.FileName = detail.Description;

            switch (detail.FileDetailType)
            {
                case "Generic":
                case "Pdf":
                case "Limited":
                case "ClearedCheck":
                    results.Add(detail);
                    break;
            }
        }

        return results;
    }
}
