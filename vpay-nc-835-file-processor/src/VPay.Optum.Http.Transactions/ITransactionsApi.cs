using System.Threading;
using System.Threading.Tasks;
using Refit;
using VPay.Optum.Http.Transactions.Models;

namespace VPay.Optum.Http.Transactions
{
    public interface ITransactionsApi
    {
        [Get("/web/services/getNfPyDt")]
        Task<TransactionInfo> GetByAchRequestHeaderId([Query, AliasAs("achfchdrid")] int achRequestHeaderId, [Query, AliasAs("size")] int size = 10000, [Query, AliasAs("from")] int? from = null, CancellationToken cancellationToken = default);

        [Get("/web/services/getNfPyDt")]
        Task<TransactionInfo> GetByTransactionId([Query, AliasAs("setxid")] long transactionId, CancellationToken cancellationToken = default);
    }
}
