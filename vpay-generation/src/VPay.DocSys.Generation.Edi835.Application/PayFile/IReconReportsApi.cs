using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Generation.Edi835.Application.Models;

namespace VPay.DocSys.Generation.Edi835.Application.PayFile
{
    public interface IReconReportsApi
    {
        Task<BankAccount> GetMain835Account(CancellationToken cancellationToken);

        Task<TransactionBankAccount> GetBankAccountForTransactionId(long transactionId, CancellationToken cancellationToken);
    }
}
