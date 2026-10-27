using System.Data;
using Dapper;
using Polly;

namespace VPay.Gateway.Data;

public static partial class DapperHelpers
{
    internal static Task<SqlMapper.GridReader> QueryMultipleAsync(
        this IDbConnection cnn,
        string sql,
        object? param,
        IDbTransaction? transaction,
        int? commandTimeout,
        CommandType? commandType,
        IAsyncPolicy? policy = null,
        CancellationToken cancellationToken = default)
    {
        if (policy == null || transaction != null)
        {
            var def = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
            return cnn.QueryMultipleAsync(def);
        }

        var context = new Context
        {
            ["sql"] = sql,
            ["conn"] = cnn
        };

        return policy.ExecuteAsync((_, t) =>
        {
            var def = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: t);
            return cnn.QueryMultipleAsync(def);
        }, context, cancellationToken);
    }

    internal static Task<IEnumerable<T>> QueryAsync<T>(
        this IDbConnection cnn,
        string sql,
        object? param,
        IDbTransaction? transaction,
        int? commandTimeout,
        CommandType? commandType,
        IAsyncPolicy? policy = null,
        CancellationToken cancellationToken = default)
    {
        if (policy == null || transaction != null)
        {
            var def = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
            return cnn.QueryAsync<T>(def);
        }

        var context = new Context
        {
            ["sql"] = sql,
            ["conn"] = cnn
        };

        return policy.ExecuteAsync((_, t) =>
        {
            var def = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: t);
            return cnn.QueryAsync<T>(def);
        }, context, cancellationToken);
    }

    internal static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(
        this IDbConnection cnn,
        string sql,
        Func<TFirst, TSecond, TReturn> map,
        object? param,
        IDbTransaction? transaction,
        int? commandTimeout,
        CommandType? commandType,
        IAsyncPolicy? policy = null,
        string splitOn = "Id",
        CancellationToken cancellationToken = default)
    {
        if (policy == null || transaction != null)
        {
            var def = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
            return cnn.QueryAsync<TFirst, TSecond, TReturn>(def, map, splitOn);
        }

        var context = new Context
        {
            ["sql"] = sql,
            ["conn"] = cnn
        };

        return policy.ExecuteAsync((_, t) =>
        {
            var def = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: t);
            return cnn.QueryAsync<TFirst, TSecond, TReturn>(def, map, splitOn);
        }, context, cancellationToken);
    }

    internal static Task<T> QueryFirstOrDefaultAsync<T>(
        this IDbConnection cnn,
        string sql,
        object? param,
        IDbTransaction? transaction,
        int? commandTimeout,
        CommandType? commandType,
        IAsyncPolicy? policy = null,
        CancellationToken cancellationToken = default)
    {
        if (policy == null || transaction != null)
        {
            var def = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
            return cnn.QueryFirstOrDefaultAsync<T>(def);
        }

        var context = new Context
        {
            ["sql"] = sql,
            ["conn"] = cnn
        };

        return policy.ExecuteAsync((_, t) =>
        {
            var def = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: t);
            return cnn.QueryFirstOrDefaultAsync<T>(def);
        }, context, cancellationToken);
    }

    internal static Task<T> ExecuteScalarAsync<T>(
        this IDbConnection cnn,
        string sql,
        object? param,
        IDbTransaction? transaction,
        int? commandTimeout,
        CommandType? commandType,
        IAsyncPolicy? policy = null,
        CancellationToken cancellationToken = default)
    {
        if (policy == null || transaction != null)
        {
            var def = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
            return cnn.ExecuteScalarAsync<T>(def);
        }

        var context = new Context
        {
            ["sql"] = sql,
            ["conn"] = cnn
        };

        return policy.ExecuteAsync((_, t) =>
        {
            var def = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: t);
            return cnn.ExecuteScalarAsync<T>(def);
        }, context, cancellationToken);
    }

    internal static Task<int> ExecuteAsync(
        this IDbConnection cnn,
        string sql,
        object? param,
        IDbTransaction? transaction,
        int? commandTimeout,
        CommandType? commandType,
        IAsyncPolicy? policy = null,
        CancellationToken cancellationToken = default)
    {
        if (policy == null || transaction != null)
        {
            var def = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
            return cnn.ExecuteAsync(def);
        }

        var context = new Context
        {
            ["sql"] = sql,
            ["conn"] = cnn
        };

        return policy.ExecuteAsync((_, t) =>
        {
            var def = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: t);
            return cnn.ExecuteAsync(def);
        }, context, cancellationToken);
    }
}
