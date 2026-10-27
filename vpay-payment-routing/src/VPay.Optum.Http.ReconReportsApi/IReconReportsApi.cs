using System.Threading;
using System.Threading.Tasks;
using Refit;
using VPay.Optum.Http.ReconReportsApi.Models;

namespace VPay.Optum.Http.ReconReportsApi
{
    public interface IReconReportsApi
    {
        [Get("/api/bank-accounts/835-metabank")]
        Task<BankAccountDto> Get835MetabankAccount(CancellationToken token = default);

        [Get("/api/transactions/{transactionId}/bank-account")]
        Task<TransactionBankAccountDto> GetTransactionBankAccount(long transactionId, CancellationToken token = default);
    }
}
