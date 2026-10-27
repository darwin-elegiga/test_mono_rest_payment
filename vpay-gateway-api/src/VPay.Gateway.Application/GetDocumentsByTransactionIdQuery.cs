using MediatR;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Common.Documents;
using VPay.Gateway.Common.Documents.Models;
using VPay.Gateway.Common.Kubera;
using VPay.Gateway.Common.PlatformConfig;

namespace VPay.Gateway.Application;

public class GetDocumentsByTransactionIdQuery : IRequest<IReadOnlyCollection<ArchiveDetail>?>
{
    public GetDocumentsByTransactionIdQuery(long transactionId)
    {
        TransactionId = transactionId;
    }

    public long TransactionId { get; }
}

public class GetDocumentsByTransactionIdQueryHandler : IRequestHandler<GetDocumentsByTransactionIdQuery, IReadOnlyCollection<ArchiveDetail>?>
{
    private readonly IMediator _mediator;
    private readonly IDocumentsApi _documentsApi;
    private readonly IUserInfo _userInfo;

    public GetDocumentsByTransactionIdQueryHandler(IMediator mediator, IDocumentsApi documentsApi, IUserInfo userInfo)
    {
        _mediator = mediator;
        _documentsApi = documentsApi;
        _userInfo = userInfo;
    }

    public async Task<IReadOnlyCollection<ArchiveDetail>?> Handle(GetDocumentsByTransactionIdQuery request, CancellationToken cancellationToken)
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

        List<ArchiveDetail> results = new List<ArchiveDetail>();
        BillingEntityDto? billingEntity = null;

        foreach (var detail in files.SelectMany(x => x.ArchiveDetails))
        {
            switch (detail.FileDetailType)
            {
                case "Generic":
                case "Pdf":
                case "Limited":
                case "ClearedCheck":
                    results.Add(detail);
                    break;

                case "FaxDocument":
                    if (billingEntity == null)
                    {
                        billingEntity = await _mediator.Send(new BillingEntityQuery(trans.ClientCode, trans.BillingEntityCode), cancellationToken).ConfigureAwait(false);
                    }
                    if (billingEntity.IsWarranty())
                    {
                        results.Add(detail);
                    }
                    //need check if BE for transaction is Warranty
                    break;
            }
        }

        return results;
    }
}
