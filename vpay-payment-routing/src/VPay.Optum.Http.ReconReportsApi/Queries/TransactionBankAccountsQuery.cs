using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Refit;
using VPay.Extensions.Caching.Memory;
using VPay.Optum.Http.ReconReportsApi.Models;

namespace VPay.Optum.Http.ReconReportsApi.Queries
{
    public class TransactionBankAccountsQuery : IRequest<TransactionBankAccounts>
    {
        public TransactionBankAccountsQuery(long transactionId)
        {
            TransactionId = transactionId;
        }

        public long TransactionId { get; }
    }

    public class TransactionBankAccountsQueryHandler : IRequestHandler<TransactionBankAccountsQuery, TransactionBankAccounts>
    {
        private readonly IReconReportsApi _reconReportsApi;
        private readonly IMemoryCache<BankAccountDto> _cache;

        public TransactionBankAccountsQueryHandler(IReconReportsApi reconReportsApi, IMemoryCache<BankAccountDto> cache)
        {
            _reconReportsApi = reconReportsApi ?? throw new ArgumentNullException(nameof(reconReportsApi));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        private async Task<BankAccountDto> GetFromAccount(CancellationToken cancellationToken = default)
        {
            string cacheKey = $"OPTUM_835BANK_KEY";

            // if the request is for the current date, we'll cache the result
            return await _cache
                .GetOrCreateAsync(
                    cacheKey,
                    () => LoadBankAccount(cancellationToken),
                    new MemoryCacheEntryOptions().SetAbsoluteExpiration(DateTime.Today.AddDays(1)),
                    false)
                .ConfigureAwait(false);

            async Task<BankAccountDto> LoadBankAccount(CancellationToken cancellationToken)
            {
                try
                {
                    return await _reconReportsApi
                        .Get835MetabankAccount(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (ApiException apiEx) when (apiEx.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
            }
        }

        private async Task<TransactionBankAccountDto> GetToAccount(TransactionBankAccountsQuery request, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _reconReportsApi
                    .GetTransactionBankAccount(request.TransactionId, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ApiException apiEx) when (apiEx.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<TransactionBankAccounts> Handle(TransactionBankAccountsQuery request, CancellationToken cancellationToken)
        {
            var fromAccount = await GetFromAccount(cancellationToken).ConfigureAwait(false);
            var toAccount = await GetToAccount(request, cancellationToken).ConfigureAwait(false);

            return new TransactionBankAccounts
            {
                ToAccountNumber = toAccount.AccountNumber,
                ToRouteNumber = toAccount.RouteNumber,
                FromAccountNumber = fromAccount.AccountNumber,
                FromRouteNumber = fromAccount.RouteNumber
            };
        }
    }
}
