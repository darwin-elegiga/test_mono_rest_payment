using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Common.Models.Sql;

namespace VPay.DocSys.Generation.Data
{
    public interface IDocumentPaymentTransactionRepository : IDataRepository
    {
        /// <summary>
        /// Return <see cref="PaymentTransaction"/>s for the given <paramref name="transactionId"/>;s <see cref="DateTime.Date"/>
        /// </summary>
        /// <param name="transactionId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<PaymentTransaction> GetPaymentTransactionsAsync(long transactionId, CancellationToken cancellationToken = default);

    }
}
