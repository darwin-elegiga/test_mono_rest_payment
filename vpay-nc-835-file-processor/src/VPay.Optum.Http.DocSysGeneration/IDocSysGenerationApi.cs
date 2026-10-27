using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Refit;
using VPay.Optum.Http.DocSysGeneration.Models;

namespace VPay.Optum.Http.DocSysGeneration
{
    public interface IDocSysGenerationApi
    {
        /// <summary>
        /// Publish the transactions through the api.
        /// </summary>
        /// <param name="entity">The entity containing the data to post.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
        /// <returns>A dictionary where the key is the transaction id and the value is the "status" for that item.</returns>
        [Post("/api/Edi835Support/PublishTransactions")]
        Task<Dictionary<long, string>> PublishTransactions([Body] Edi835TransactionsDto entity, CancellationToken cancellationToken);
    }
}
