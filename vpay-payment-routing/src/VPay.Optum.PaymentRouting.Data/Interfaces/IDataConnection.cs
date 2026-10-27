using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace VPay.Optum.PaymentRouting.Data.Abstractions
{
    /* Boiler plate code copied from VPay.ReconReports master/v0.4.0/Commit 30a20b19 */
    /// <inheritdoc />
    /// <summary>
    /// This will be used to pass in the connection for each call
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IDataConnection<T> : IDisposable where T : IDbConnection
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
        IDbTransaction CurrentTransaction { get; }

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

        Task<IDataConnectionTransaction> BeginTransaction();

        Task<IDataConnectionTransaction> BeginTransaction(IsolationLevel isolationLevel);

        /// <summary>
        /// Execute a parameterized stored procedure that returns the number of rows affected.
        /// </summary>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="parameters">The parameters to pass, if any.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// /// <returns>The number of rows affected.</returns>
        [ExcludeFromCodeCoverage]
        public Task<int> SaveExecuteAsync(string sql, object parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentException(nameof(sql) + " cannot be empty", nameof(sql));
            }

            return this.SaveExecuteAsync(sql, parameters, CurrentTransaction, DefaultCommandTimeout, CommandType.StoredProcedure, cancellationToken);
        }

        /// <summary>
        /// Execute a parameterized stored procedure that returns a single record.
        /// </summary>
        /// <typeparam name="TResult">The type of results to return.</typeparam>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="parameters">The parameters to pass, if any.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage]
        public Task<TResult> SaveFirstOrDefaultAsync<TResult>(string sql, object parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentException(nameof(sql) + " cannot be empty", nameof(sql));
            }

            return this.SaveFirstOrDefaultAsync<TResult, T>(sql, parameters, CurrentTransaction, DefaultCommandTimeout, CommandType.StoredProcedure, cancellationToken);
        }

        /// <summary>
        /// Execute a parameterized stored procedure that returns a single value.
        /// </summary>
        /// <typeparam name="TResult">The type of results to return.</typeparam>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="parameters">The parameters to pass, if any.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The first cell returned, as <typeparamref name="T"/>.</returns>
        [ExcludeFromCodeCoverage]
        public Task<TResult> SaveExecuteScalarAsync<TResult>(string sql, object parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentException(nameof(sql) + " cannot be empty", nameof(sql));
            }

            return this.SaveExecuteScalarAsync<TResult, T>(sql, parameters, CurrentTransaction, DefaultCommandTimeout, CommandType.StoredProcedure, cancellationToken);
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
        public async Task<IEnumerable<TResult>> GetAsync<TResult>(string sql, object parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentException(nameof(sql) + " cannot be empty", nameof(sql));
            }

            return await this.GetAsync<TResult, T>(sql, parameters, CurrentTransaction, DefaultCommandTimeout, CommandType.StoredProcedure, cancellationToken);
        }

        /// <summary>
        /// Execute a command that returns multiple result sets, and access each in turn.
        /// </summary>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="parameters">The parameters to pass, if any.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>SqlMapper.GridReader</returns>
        [ExcludeFromCodeCoverage]
        public async Task<IGridAccessor> GetMultipleAsync(string sql, object parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentException(nameof(sql) + " cannot be empty", nameof(sql));
            }

            var gridReader = await this.GetMultipleAsync(sql, parameters, CurrentTransaction, DefaultCommandTimeout, CommandType.StoredProcedure, cancellationToken);
            return new GridAccessor(gridReader);
        }

        /// <summary>
        /// Execute a single-row query asynchronously
        /// </summary>
        /// <typeparam name="TResult">The type of results to return.</typeparam>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="parameters">The parameters to pass, if any.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage]
        public Task<TResult> GetFirstOrDefaultAsync<TResult>(string sql, object parameters, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentException(nameof(sql) + " cannot be empty", nameof(sql));
            }

            return this.GetFirstOrDefaultAsync<TResult, T>(sql, parameters, CurrentTransaction, DefaultCommandTimeout, CommandType.StoredProcedure, cancellationToken);
        }

        /// <summary>
        /// Execute parameterized SQL that selects a single value.
        /// </summary>
        /// <typeparam name="TResult">The type of results to return.</typeparam>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="parameters">The parameters to pass, if any.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The first cell returned, as <typeparamref name="T"/>.</returns>
        [ExcludeFromCodeCoverage]
        public Task<TResult> GetExecuteScalarAsync<TResult>(string sql, object parameters, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentException(nameof(sql) + " cannot be empty", nameof(sql));
            }

            return this.GetExecuteScalarAsync<TResult, T>(sql, parameters, CurrentTransaction, DefaultCommandTimeout, CommandType.StoredProcedure, cancellationToken);
        }
    }
}
