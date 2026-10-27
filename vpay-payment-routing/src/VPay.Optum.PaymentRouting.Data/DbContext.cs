using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data
{
    /* Boiler plate code copied from VPay.ReconReports master/v0.4.0/Commit 30a20b19 */

    [ExcludeFromCodeCoverage]
    public class DbContext : IDbContext
    {
        private readonly IDataConnection<SqlConnection> _connection;
        private readonly IServiceProvider _serviceProvider;

        public DbContext(IDataConnection<SqlConnection> connection, IServiceProvider serviceProvider)
        {
            _connection = connection;
            _serviceProvider = serviceProvider;
        }

        public T GetRepository<T>() where T : IBaseRepository
        {
            return (T)_serviceProvider.GetService(typeof(T));
        }

        public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
        {
            return _connection.CanConnectAsync(cancellationToken);
        }

        public Task<IDataConnectionTransaction> BeginTransaction(IsolationLevel isolationLevel)
        {
            return _connection.BeginTransaction(isolationLevel);
        }

        public Task<IDataConnectionTransaction> BeginTransaction()
        {
            return _connection.BeginTransaction();
        }
    }
}