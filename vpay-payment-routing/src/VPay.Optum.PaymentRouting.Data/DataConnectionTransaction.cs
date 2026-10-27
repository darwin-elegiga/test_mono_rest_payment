using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace VPay.Optum.PaymentRouting.Data.Abstractions
{
    /* Boiler plate code copied from VPay.ReconReports master/v0.4.0/Commit 30a20b19 */

    [ExcludeFromCodeCoverage]
    public class DataConnectionTransaction<T> : IDataConnectionTransaction where T : DbConnection
    {
        private readonly DataConnection<T> _dataConnection;
        public IDbTransaction Transaction { get; private set; }

        public DataConnectionTransaction(DataConnection<T> dataConnection, DbTransaction transaction)
        {
            _dataConnection = dataConnection;
            Transaction = transaction;
            _dataConnection.UseTransaction(this);
        }

        public void Dispose()
        {
            if (Transaction != null)
            {
                Transaction.Dispose();
                Transaction = null;
                _dataConnection.UseTransaction(null);
            }
        }

        public Guid TransactionId { get; } = Guid.NewGuid();

        public void Commit()
        {
            Transaction.Commit();
        }

        public void Rollback()
        {
            Transaction.Rollback();
            Transaction = null;
            _dataConnection.UseTransaction(null);
        }
    }
}
