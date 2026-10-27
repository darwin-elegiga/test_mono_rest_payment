using System.Data;
using Dapper;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data;

public static partial class DapperHelpers
{
    /// <summary>
    /// Execute a query asynchronously using .NET 4.5 Task.
    /// </summary>
    /// <typeparam name="TConn"></typeparam>
    /// <param name="cnn">The connection to query on.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="transaction">The transaction to use, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// Execute a command that returns multiple result sets, and access each in turn.
    /// </returns>
    public static async Task<SqlMapper.GridReader> GetMultipleAsync<TConn>(
        this IDataConnection<TConn> cnn,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default) where TConn : IDbConnection
    {
        var timeout = commandTimeout ?? cnn.DefaultCommandTimeout;
        var connection = await cnn.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var retryPolicy = cnn.RetryPolicy?.GetStandardRetryPolicy();
        transaction ??= cnn.CurrentTransaction;

        return await QueryMultipleAsync(connection, sql, param, transaction, timeout, commandType, retryPolicy, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Execute a query asynchronously using .NET 4.5 Task.
    /// </summary>
    /// <typeparam name="T">The type of results to return.</typeparam>
    /// <typeparam name="TConn"></typeparam>
    /// <param name="cnn">The connection to query on.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="transaction">The transaction to use, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// A sequence of data of <typeparamref name="T"/>; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
    /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
    /// </returns>
    public static async Task<IEnumerable<T>> GetAsync<T, TConn>(
        this IDataConnection<TConn> cnn,
        string sql, object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default) where TConn : IDbConnection
    {
        var timeout = commandTimeout ?? cnn.DefaultCommandTimeout;
        var connection = await cnn.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var retryPolicy = cnn.RetryPolicy?.GetStandardRetryPolicy();
        transaction ??= cnn.CurrentTransaction;

        return await QueryAsync<T>(connection, sql, param, transaction, timeout, commandType, retryPolicy, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Execute a single-row query asynchronously using .NET 4.5 Task.
    /// </summary>
    /// <typeparam name="T">The type of results to return.</typeparam>
    /// <typeparam name="TConn"></typeparam>
    /// <param name="cnn">The connection to query on.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="transaction">The transaction to use, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<T> GetFirstOrDefaultAsync<T, TConn>(
        this IDataConnection<TConn> cnn,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default) where TConn : IDbConnection
    {
        var timeout = commandTimeout ?? cnn.DefaultCommandTimeout;
        var connection = await cnn.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var retryPolicy = cnn.RetryPolicy?.GetStandardRetryPolicy();
        transaction ??= cnn.CurrentTransaction;

        return await QueryFirstOrDefaultAsync<T>(connection, sql, param, transaction, timeout, commandType, retryPolicy, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Execute a single-row query asynchronously using .NET 4.5 Task.
    /// </summary>
    /// <typeparam name="TFirst">The first type in the record set.</typeparam>
    /// <typeparam name="TSecond">The second type in the record set.</typeparam>
    /// <typeparam name="TConn"></typeparam>
    /// <typeparam name="TReturn">The combined type to return.</typeparam>
    /// <param name="cnn">The connection to query on.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="map">The function to map row types to the return type.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="transaction">The transaction to use, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<TReturn> GetFirstOrDefaultAsync<TFirst, TSecond, TConn, TReturn>(
        this IDataConnection<TConn> cnn,
        string sql,
        Func<TFirst, TSecond, TReturn> map,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        string splitOn = "Id",
        CancellationToken cancellationToken = default) where TConn : IDbConnection
    {
        var timeout = commandTimeout ?? cnn.DefaultCommandTimeout;
        var connection = await cnn.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var retryPolicy = cnn.RetryPolicy?.GetStandardRetryPolicy();
        transaction ??= cnn.CurrentTransaction;

        var items = await QueryAsync(connection, sql, map, param, transaction, timeout, commandType, retryPolicy, splitOn, cancellationToken).ConfigureAwait(false);

        return items.FirstOrDefault();
    }

    /// <summary>
    /// Execute parameterized SQL that selects a single value.
    /// </summary>
    /// <typeparam name="T">The type of results to return.</typeparam>
    /// <typeparam name="TConn"></typeparam>
    /// <param name="cnn">The connection to query on.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="transaction">The transaction to use, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The first cell returned, as <typeparamref name="T"/>.</returns>
    public static async Task<T> GetExecuteScalarAsync<T, TConn>(this IDataConnection<TConn> cnn,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default) where TConn : IDbConnection
    {
        var timeout = commandTimeout ?? cnn.DefaultCommandTimeout;
        var connection = await cnn.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var retryPolicy = cnn.RetryPolicy?.GetStandardRetryPolicy();
        transaction ??= cnn.CurrentTransaction;

        return await ExecuteScalarAsync<T>(connection, sql, param, transaction, timeout, commandType, retryPolicy, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Execute a command asynchronously using .NET 4.5 Task.
    /// </summary>
    /// <typeparam name="TConn"></typeparam>
    /// <param name="cnn">The connection to query on.</param>
    /// <param name="sql">The SQL to execute for the query.</param>
    /// <param name="param">The parameters to pass, if any.</param>
    /// <param name="transaction">The transaction to use, if any.</param>
    /// <param name="commandTimeout">The command timeout (in seconds).</param>
    /// <param name="commandType">The type of command to execute.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The number of rows affected.</returns>
    public static async Task<int> GetExecuteAsync<TConn>(this IDataConnection<TConn> cnn,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default) where TConn : IDbConnection
    {
        var timeout = commandTimeout ?? cnn.DefaultCommandTimeout;
        var connection = await cnn.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var retryPolicy = cnn.RetryPolicy?.GetStandardRetryPolicy();
        transaction ??= cnn.CurrentTransaction;

        return await ExecuteAsync(connection, sql, param, transaction, timeout, commandType, retryPolicy, cancellationToken).ConfigureAwait(false);
    }
}
