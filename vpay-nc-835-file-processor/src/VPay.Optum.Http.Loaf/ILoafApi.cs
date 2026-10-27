using System.Threading;
using System.Threading.Tasks;
using Refit;
using VPay.Optum.Http.Loaf.Models;

namespace VPay.Optum.Http.Loaf
{
    [Headers("accept: application/json")]
    public interface ILoafApi
    {
        [Put("/api/OptumBankInfo")]
        Task<TransactionDetails> Upsert([Body] TransactionBankInfoUpsert entity, CancellationToken cancellationToken);
    }
}
