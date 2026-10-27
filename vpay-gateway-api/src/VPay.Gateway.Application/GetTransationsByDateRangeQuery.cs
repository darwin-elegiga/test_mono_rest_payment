using System.Globalization;
using MediatR;
using Vpay.Gateway.Common.Vpay;
using Vpay.Gateway.Common.Vpay.Models;
using VPay.Gateway.Common.Documents.Models;
using VPay.Gateway.Data.Paging;

namespace VPay.Gateway.Application;
public class GetTransactionsByDateRangeQuery : IRequest<PagedResult<TransactionDetails>>
{
    public string TaxId { get; set; }
    public string PaymentId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }

    public GetTransactionsByDateRangeQuery(string paymentId, DateTime startDate, DateTime endDate, string taxId, int? page = Defaults.DefaultPage, int? pageSize =  Defaults.DefaultPageSize)
    {
        PaymentId = paymentId;
        StartDate = startDate;
        EndDate = endDate;
        TaxId = taxId;
        Page = page;
        PageSize = pageSize;
    }
}

public class GetTransactionsByDateRangeQueryHandler : IRequestHandler<GetTransactionsByDateRangeQuery, PagedResult<TransactionDetails>>
{
    private readonly IVPayApi _api;

    public GetTransactionsByDateRangeQueryHandler(IVPayApi api)
    {
        _api = api;
    }

    public async Task<PagedResult<TransactionDetails>> Handle(GetTransactionsByDateRangeQuery request, CancellationToken cancellationToken)
    {
        List<TransactionDetails> transactionDetails = new();
        int page = request.Page ?? Defaults.DefaultPage;
        int pageSize = request.PageSize ?? Defaults.DefaultPageSize;
        var trxSearch = new TransactionSearchRequest
        {
            StartDate = request.StartDate.ToUniversalTime(),
            EndDate = request.EndDate.ToUniversalTime(),
            Term = request.TaxId != null ? $"paymentId:{request.PaymentId},taxId:{request.TaxId}" : $"paymentId:{request.PaymentId}",
            Statuses = new List<string>(1) { "LOADED" }           
        };

        try
        {
            var result = await _api.PostTransactionSearch(trxSearch, cancellationToken).ConfigureAwait(false);

            if (result?.Documents?.Count > 0)
            {
                result.Documents.ForEach(document =>
                {
                    transactionDetails.Add(new TransactionDetails()
                    {
                        VpayTransactionId = document.TransactionId,
                        PaymentDate = Convert.ToDateTime(document.DateLoaded, CultureInfo.InvariantCulture),
                        PaymentId = document.PaymentId,
                        PaymentType = document.BillingTypeCode,
                        ProviderTin = document.TaxId
                    });
                });
            }
            var transactions = transactionDetails.Skip((page -1) * pageSize).Take(pageSize);
            return new PagedResult<TransactionDetails>(transactions, page, pageSize, transactionDetails.Count);
        }
        catch (Exception)
        {
            throw;
        }
    }
}
