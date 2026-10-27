using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;


namespace VPay.Optum.PaymentRouting.Data.Abstractions
{
    /* Boiler plate code copied from VPay.ReconReports master/v0.4.0/Commit 30a20b19 */
    /// <inheritdoc />
    /// <summary>
    /// This is the base class for creating a DataConnection that should be passed into each of the Database services. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [ExcludeFromCodeCoverage]
    public abstract class DataConnection<T> : IDataConnection<T> where T : DbConnection
    {

        public int? DefaultCommandTimeout { get; set; }

        public abstract IRetryPolicyRegistry RetryPolicy { get; }


        protected ILogger Logger { get; }
        /// <summary>
        /// This is the passed in Connection string
        /// </summary>
        protected string ConnectionString { get; }

        /// <summary>
        /// This is the DbConnection 
        /// </summary>
        protected T DbConnection { get; private set; }

        public virtual IDbTransaction CurrentTransaction => DataConnectionTransaction?.Transaction;

        /// <summary>
        ///     Gets the current transaction.
        /// </summary>
        public virtual DataConnectionTransaction<T> DataConnectionTransaction { get; protected set; }

        /// <summary>
        /// Instantiates a new DataConnection class with the connectionString
        /// </summary>
        /// <param name="connectionString">This is a required field</param>
        /// <param name="logger">This is the logger used in the application</param>
        /// <param name="policy">Polly policy for when you try to open a connection</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        protected DataConnection(string connectionString, ILogger logger)
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                throw new ArgumentException("Must have a value", nameof(connectionString));
            }

            Logger = logger ?? new NullLogger<DataConnection<T>>();
        }

        /// <summary>
        /// This will create a connection object
        /// </summary>
        /// <returns></returns>
        protected abstract T GetConnectionObject();

        /// <inheritdoc />
        public async Task<T> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (DbConnection == null)
            {
                DbConnection = GetConnectionObject();
                await OpenAsync(cancellationToken);
            }
            else if (DbConnection.State == ConnectionState.Closed)
            {
                await OpenAsync(cancellationToken);
            }

            return DbConnection;
        }

        public async Task<IDataConnectionTransaction> BeginTransaction()
        {
            if (CurrentTransaction != null)
            {
                throw new InvalidOperationException("Transaction is already started");
            }

            var connection = await GetOpenConnectionAsync();

            var transaction = connection.BeginTransaction();

            var result = new DataConnectionTransaction<T>(this, transaction);

            return result;
        }

        public async Task<IDataConnectionTransaction> BeginTransaction(IsolationLevel isolationLevel)
        {
            if (CurrentTransaction != null)
            {
                throw new InvalidOperationException("Transaction is already started");
            }

            var connection = await GetOpenConnectionAsync();

            var transaction = connection.BeginTransaction(isolationLevel);

            var result = new DataConnectionTransaction<T>(this, transaction);

            return result;
        }

        private async Task OpenAsync(CancellationToken cancellationToken)
        {
            var policy = RetryPolicy?.GetOpenConnectionRetryPolicy();
            if (policy == null)
            {
                await DbConnection.OpenAsync(cancellationToken);
            }
            else
            {
                await policy.ExecuteAsync(t => DbConnection.OpenAsync(t), cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var connection = await GetOpenConnectionAsync(cancellationToken);
                return connection.State == ConnectionState.Open;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Could not connect to the Sql Database");
                return false;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            CurrentTransaction?.Dispose();

            DbConnection?.Close();
            DbConnection?.Dispose();
        }


        /// <summary>
        ///     Specifies an existing <see cref="DbTransaction" /> to be used for database operations.
        /// </summary>
        /// <param name="transaction"> The transaction to be used. </param>
        internal virtual IDataConnectionTransaction UseTransaction(DataConnectionTransaction<T> transaction)
        {
            if (transaction == null)
            {
                if (DataConnectionTransaction != null)
                {
                    DataConnectionTransaction = null;
                }
            }
            else
            {
                if (CurrentTransaction != null)
                {
                    throw new InvalidOperationException("Transaction is already started");
                }

                DataConnectionTransaction = transaction;
            }

            return DataConnectionTransaction;
        }

    }
}
