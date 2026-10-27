using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Generation.Common.Models.Sql;

namespace VPay.DocSys.Generation.Data;

public interface IDb2TransactionRepository
{
    Task<PaymentTransaction> GetPayerInfoByTransactionIdAsync(long transactionId, CancellationToken cancellationToken = default);
}
