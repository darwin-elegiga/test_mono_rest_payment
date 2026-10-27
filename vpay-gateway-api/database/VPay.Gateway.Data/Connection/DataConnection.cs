using System.Data;
using System.Data.Common;
using Dapper;
using Polly;
using VPay.Gateway.Data.Paging;
using VPay.Gateway.Data.QueryOptions;

namespace VPay.Gateway.Data.Connection;

/// <summary>
/// This is the base class for creating a DataConnection that should be passed into each of the Database services.
/// </summary>
/// <typeparam name="TDbConnection"></typeparam>
public abstract class DataConnection<TDbConnection> : IDataConnection<TDbConnection> where TDbConnection : DbConnection
{
    /// <summary>
    /// Instantiates a new DataConnection class with the connectionString
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="connectionString"/> is null.</exception>
    /// <exception cref="ArgumentException">If the <paramref name="connectionString"/> is an empty string or a string containing only whitespace.</exception>
    protected DataConnection(string? connectionString)
    {
        ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Must not be empty or whitespace", nameof(connectionString));
        }
    }

    public int? DefaultCommandTimeout { get; set; }

    public abstract IRetryPolicyRegistry RetryPolicy { get; }

    /// <summary>
    /// This is the passed in Connection string
    /// </summary>
    protected string ConnectionString { get; }

    /// <summary>
    /// This is the DbConnection
    /// </summary>
    protected TDbConnection? DbConnection { get; private set; }

    public virtual IDbTransaction? CurrentTransaction => DataConnectionTransaction?.Transaction;

    /// <summary>
    ///     Gets the current transaction.
    /// </summary>
    public virtual DataConnectionTransaction<TDbConnection>? DataConnectionTransaction { get; protected set; }

    /// <summary>
    /// This will create a connection object
    /// </summary>
    protected abstract TDbConnection GetConnectionObject();

    /// <inheritdoc />
    public async Task<TDbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (DbConnection == null)
        {
            DbConnection = GetConnectionObject();
            await OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        else if (DbConnection.State == ConnectionState.Closed)
        {
            await OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        return DbConnection;
    }

    public async Task<IDataConnectionTransaction> BeginTransaction()
    {
        if (CurrentTransaction != null)
        {
            throw new InvalidOperationException("Transaction is already started");
        }

        TDbConnection connection = await GetOpenConnectionAsync().ConfigureAwait(false);
        DbTransaction transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

        return new DataConnectionTransaction<TDbConnection>(this, transaction);
    }

    public async Task<IDataConnectionTransaction> BeginTransaction(IsolationLevel isolationLevel)
    {
        if (CurrentTransaction != null)
        {
            throw new InvalidOperationException("Transaction is already started");
        }

        TDbConnection connection = await GetOpenConnectionAsync().ConfigureAwait(false);
        DbTransaction transaction = await connection.BeginTransactionAsync(isolationLevel).ConfigureAwait(false);

        return new DataConnectionTransaction<TDbConnection>(this, transaction);
    }

    private async Task OpenAsync(CancellationToken cancellationToken)
    {
        if (DbConnection == null)
        {
            return;
        }

        IAsyncPolicy policy = RetryPolicy.GetOpenConnectionRetryPolicy();
        if (policy == null)
        {
            await DbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await policy.ExecuteAsync(t => DbConnection.OpenAsync(t), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        TDbConnection connection = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return connection.State == ConnectionState.Open;
    }

    /// <summary>
    ///     Specifies an existing <see cref="DbTransaction" /> to be used for database operations.
    /// </summary>
    /// <param name="transaction"> The transaction to be used. </param>
    internal virtual IDataConnectionTransaction? UseTransaction(DataConnectionTransaction<TDbConnection>? transaction)
    {
        if (transaction == null)
        {
            DataConnectionTransaction = null;
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

    #region Read Queries

    /// <summary>
    /// Execute a sql statement to return a single-row asynchronously.
    /// </summary>
    /// <typeparam name="TModel">The type of results to return.</typeparam>
    public async Task<TModel> GetSingleOrDefaultAsync<TModel>(string sql, object? parameters, CommandType commandType, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentException(nameof(sql) + " cannot be empty", nameof(sql));
        }

        var databaseConnection = await GetOpenConnectionAsync(cancellationToken);
        var retryPolicy = RetryPolicy.GetStandardRetryPolicy();

        return await databaseConnection.QuerySingleOrDefaultAsync<TModel>(sql, parameters, CurrentTransaction, DefaultCommandTimeout, commandType, retryPolicy, cancellationToken);
    }

    /// <summary>
    /// Execute a stored procedure to return a single-row asynchronously.
    /// </summary>
    /// <typeparam name="TModel">The type of results to return.</typeparam>
    public async Task<TModel> GetSingleOrDefaultAsync<TModel>(string sql, object? parameters, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentException(nameof(sql) + " cannot be empty", nameof(sql));
        }

        var databaseConnection = await GetOpenConnectionAsync(cancellationToken);
        var retryPolicy = RetryPolicy.GetStandardRetryPolicy();

        return await databaseConnection.QuerySingleOrDefaultAsync<TModel>(sql, parameters, CurrentTransaction, DefaultCommandTimeout, CommandType.StoredProcedure, retryPolicy, cancellationToken);
    }

    /// <summary>
    /// Execute a stored procedure to return a single-row asynchronously.
    /// </summary>
    /// <typeparam name="TFirst">The first part of the results.</typeparam>
    /// <typeparam name="TSecond">The second part of the results.</typeparam>
    /// <typeparam name="TModel">The type of results to return.</typeparam>
    public async Task<TModel> GetSingleOrDefaultAsync<TFirst, TSecond, TModel>(string sql, object? parameters, Combiner<TFirst, TSecond, TModel> combiner, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentException(nameof(sql) + " cannot be empty", nameof(sql));
        }

        var databaseConnection = await GetOpenConnectionAsync(cancellationToken);
        var retryPolicy = RetryPolicy.GetStandardRetryPolicy();

        IEnumerable<TModel> items = await databaseConnection.QueryAsync(sql, combiner.Combine, parameters, CurrentTransaction, DefaultCommandTimeout, CommandType.StoredProcedure, retryPolicy, combiner.SplitOn, cancellationToken);

        return items.SingleOrDefault()!;
    }

    /// <summary>
    /// Execute a stored procedure to return multiple rows asynchronously.
    /// </summary>
    /// <typeparam name="TModel">The type of results to return.</typeparam>
    public async Task<PagedResult<TModel>> GetPagedResultAsync<TModel>(string sql, object? parameters, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentException(nameof(sql) + " cannot be empty", nameof(sql));
        }

        TDbConnection databaseConnection = await GetOpenConnectionAsync(cancellationToken);
        var retryPolicy = RetryPolicy.GetStandardRetryPolicy();

        SqlMapper.GridReader response = await databaseConnection.QueryMultipleAsync(sql, parameters, CurrentTransaction, DefaultCommandTimeout, CommandType.StoredProcedure, retryPolicy, cancellationToken);
        List<TModel> items = response.Read<TModel>().ToList();
        PageModel pageInfo = response.ReadFirst<PageModel>();

        return new PagedResult<TModel>(items, pageInfo);
    }

    /// <summary>
    /// Execute a stored procedure to return multiple rows asynchronously.
    /// </summary>
    /// <typeparam name="TFirst">The first part of the results.</typeparam>
    /// <typeparam name="TSecond">The second part of the results.</typeparam>
    /// <typeparam name="TModel">The type of results to return.</typeparam>
    public async Task<PagedResult<TModel>> GetPagedResultAsync<TFirst, TSecond, TModel>(string sql, object? parameters, Combiner<TFirst, TSecond, TModel> combiner, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentException(nameof(sql) + " cannot be empty", nameof(sql));
        }

        TDbConnection databaseConnection = await GetOpenConnectionAsync(cancellationToken);
        var retryPolicy = RetryPolicy.GetStandardRetryPolicy();

        SqlMapper.GridReader response = await databaseConnection.QueryMultipleAsync(sql, parameters, CurrentTransaction, DefaultCommandTimeout, CommandType.StoredProcedure, retryPolicy, cancellationToken);
        List<TModel> items = response.Read(combiner.Combine, combiner.SplitOn).ToList();
        PageModel pageInfo = response.ReadFirst<PageModel>();

        return new PagedResult<TModel>(items, pageInfo);
    }

    #endregion Read Queries

    #region IDisposable

    private bool _disposed;

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // Protected implementation of Dispose pattern.
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            CurrentTransaction?.Dispose();

            DbConnection?.Close();
            DbConnection?.Dispose();
        }

        _disposed = true;
    }

    #endregion IDisposable
}
