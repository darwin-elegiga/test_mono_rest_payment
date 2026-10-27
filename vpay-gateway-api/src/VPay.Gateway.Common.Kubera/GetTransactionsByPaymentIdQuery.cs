using MediatR;
using Microsoft.Extensions.Caching.Memory;
using VPay.Extensions.Caching.Memory;
using VPay.Gateway.Common.Kubera.Models;

namespace VPay.Gateway.Common.Kubera;

public record GetTransactionsByPaymentIdQuery(
    string ClientCode,
    string PaymentId
) : IRequest<Transaction[]>;

public class GetTransactionsByPaymentIdQueryHandler : IRequestHandler<GetTransactionsByPaymentIdQuery, Transaction[]>
{
    private readonly IKuberaApi _api;
    private readonly IMemoryCache<Transaction[]> _transactionsCache;

    private static readonly MemoryCacheEntryOptions _cacheOptions = new MemoryCacheEntryOptions()
        .SetAbsoluteExpiration(TimeSpan.FromHours(1));

    public GetTransactionsByPaymentIdQueryHandler(IKuberaApi api, IMemoryCache<Transaction[]> transactionsCache)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _transactionsCache = transactionsCache ?? throw new ArgumentNullException(nameof(transactionsCache));
    }

    public async Task<Transaction[]> Handle(GetTransactionsByPaymentIdQuery request, CancellationToken cancellationToken)
    {
        // NOTE: This could be stored in a distributed cache, but at the time
        // this is being written we don't have a distributed cache available
        string cacheKey = $"TransactionsByPaymentId_{request.ClientCode}_{request.PaymentId}";
        var result = await _transactionsCache.GetOrCreateAsync(
            cacheKey,
            static async (api, query, cancellation) =>
            {
                var txs = await api.GetTransactionsByPaymentId(query.ClientCode, query.PaymentId, cancellationToken: cancellation).ConfigureAwait(false);
                return txs is null or { Length: 0 } ? null : txs;
            },
            _api,
            request,
            cancellationToken,
            _cacheOptions,
            cacheNullValue: false
        ).ConfigureAwait(false);

        return result ?? Array.Empty<Transaction>();
    }
}
