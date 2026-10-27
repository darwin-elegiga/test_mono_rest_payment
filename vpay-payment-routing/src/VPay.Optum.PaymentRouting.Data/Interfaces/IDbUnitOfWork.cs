using System.Data;
using System.Threading.Tasks;

namespace VPay.Optum.PaymentRouting.Data.Abstractions
{
    public interface IDbUnitOfWork
    {
        Task<IDataConnectionTransaction> BeginTransaction();

        Task<IDataConnectionTransaction> BeginTransaction(IsolationLevel isolationLevel);
    }
}
