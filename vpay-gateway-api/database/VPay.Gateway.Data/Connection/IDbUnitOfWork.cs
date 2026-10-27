using System.Data;

namespace VPay.Gateway.Data.Connection;

public interface IDbUnitOfWork
{
    Task<IDataConnectionTransaction> BeginTransaction();

    Task<IDataConnectionTransaction> BeginTransaction(IsolationLevel isolationLevel);
}
