using System.Data;
using VPay.Gateway.Data.Paging;
using VPay.Gateway.Data.QueryOptions;

namespace VPay.Gateway.Data.Connection;

/// <summary>
/// This will be used to pass in the connection for each call
/// </summary>
/// <typeparam name="T">The type of the connection.</typeparam>
public interface IDataConnection<T> : IDisposable, IDbUnitOfWork where T : IDbConnection
{
    /// <summary>
    /// The time in seconds to wait for the command to execute.
    /// </summary>
    int? DefaultCommandTimeout { get; }

    /// <summary>
    /// This is the standard Polly retry policy that each method will use when running
    /// </summary>
    IRetryPolicyRegistry RetryPolicy { get; }

    /// <summary>
    /// This is the current transaction
    /// </summary>
    IDbTransaction? CurrentTransaction { get; }

    /// <summary>
    /// This will open a connection to the database asynchronously
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>This returns an open connection of type IDbConnection</returns>
    Task<T> GetOpenConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// This will test if a connection can be opened to the database
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>This returns true if a connection to the database can be opened</returns>
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);

    #region Read Queries

    /// <summary>
    /// Execute a sql statement to return a single-row asynchronously.
    /// </summary>
    /// <typeparam name="TModel">The type of result to return.</typeparam>
    Task<TModel> GetSingleOrDefaultAsync<TModel>(string sql, object? parameters, CommandType commandType, CancellationToken cancellationToken);

    /// <summary>
    /// Execute a stored procedure to return a single-row asynchronously.
    /// </summary>
    /// <typeparam name="TModel">The type of result to return.</typeparam>
    Task<TModel> GetSingleOrDefaultAsync<TModel>(string sql, object? parameters, CancellationToken cancellationToken);

    /// <summary>
    /// Execute a stored procedure to return a single-row asynchronously.
    /// </summary>
    /// <typeparam name="TFirst">The first part of the result.</typeparam>
    /// <typeparam name="TSecond">The second part of the result.</typeparam>
    /// <typeparam name="TModel">The type of result to return.</typeparam>
    Task<TModel> GetSingleOrDefaultAsync<TFirst, TSecond, TModel>(string sql, object? parameters, Combiner<TFirst, TSecond, TModel> combiner, CancellationToken cancellationToken);

    /// <summary>
    /// Execute a stored procedure to return multiple rows asynchronously without pagination.
    /// </summary>
    /// <typeparam name="TModel">The type of results to return.</typeparam>
    public Task<IEnumerable<TModel>> GetMultipleResultAsync<TModel>(string sql, object? parameters, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentException(nameof(sql) + " cannot be empty", nameof(sql));
        }

        return this.GetAsync<TModel, T>(sql, parameters, CurrentTransaction, DefaultCommandTimeout, CommandType.StoredProcedure, cancellationToken);
    }

    /// <summary>
    /// Execute a stored procedure to return multiple rows asynchronously.
    /// </summary>
    /// <typeparam name="TModel">The type of results to return.</typeparam>
    Task<PagedResult<TModel>> GetPagedResultAsync<TModel>(string sql, object? parameters, CancellationToken cancellationToken);

    /// <summary>
    /// Execute a stored procedure to return multiple rows asynchronously.
    /// </summary>
    /// <typeparam name="TFirst">The first part of the results.</typeparam>
    /// <typeparam name="TSecond">The second part of the results.</typeparam>
    /// <typeparam name="TModel">The type of results to return.</typeparam>
    Task<PagedResult<TModel>> GetPagedResultAsync<TFirst, TSecond, TModel>(string sql, object? parameters, Combiner<TFirst, TSecond, TModel> combiner, CancellationToken cancellationToken);

    #endregion Read Queries
}
