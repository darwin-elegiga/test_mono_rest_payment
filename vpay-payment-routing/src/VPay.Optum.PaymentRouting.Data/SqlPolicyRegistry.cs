using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data
{
    /* Boiler plate code copied from VPay.ReconReports master/v0.4.0/Commit 30a20b19 */

    [ExcludeFromCodeCoverage]
    public class SqlPolicyRegistry : IRetryPolicyRegistry
    {
        private readonly PolicyRegistry _registry;
        private readonly ILogger _logger;

        private const string StandardRetryPolicy = "StandardRetryPolicy";
        private const string StandardSaveQueryRetryPolicy = "StandardSaveQueryRetryPolicy";
        private const string OpenConnectionRetryPolicy = "OpenConnectionRetryPolicy";

        public SqlPolicyRegistry(ILogger<SqlPolicyRegistry> logger)
        {
            _logger = logger;
            _registry = new PolicyRegistry();
        }

        /// <summary>
        /// This will get the policy that will retry 2 times when the following sql errors occur:
        /// Timeout errors (Number = -2) or deadlock errors (Number = 1205) or
        /// Cannot continue the execution because the session is in the kill state (Number = 596)
        /// </summary>
        /// <returns></returns>
        public IAsyncPolicy GetStandardRetryPolicy()
        {
            if (_registry.ContainsKey(StandardRetryPolicy))
            {
                return _registry.Get<IAsyncPolicy>(StandardRetryPolicy);
            }

            var waitAndRetryPolicy = Policy
                .Handle<SqlException>(x => x.Number == -2 || x.Number == 1205)
                .WaitAndRetryAsync(2,
                    retryAttempt => TimeSpan.FromMilliseconds(200 * retryAttempt),
                    onRetry: (exception, calculatedWaitDuration, retryCount, context) =>
                    {
                        var sql = context.ContainsKey("sql") ? context["sql"] : "";

                        using (_logger.BeginScope(new Dictionary<string, object> { ["sql"] = sql }))
                        {
                            _logger.LogInformation(exception, "DB Retry {RetryCount} occurred after {timeSpan}", retryCount, calculatedWaitDuration);
                        }
                    });

            // This error happens during the fail over and takes longer so have it in a different policy
            var waitAndRetryPolicyForError596 = Policy
                .Handle<SqlException>(x => x.Number == 596)
                .WaitAndRetryAsync(2,
                    retryAttempt => TimeSpan.FromSeconds(5 + retryAttempt),
                    onRetry: (exception, calculatedWaitDuration, retryCount, context) =>
                    {
                        var sql = context.ContainsKey("sql") ? context["sql"] : "";
                        using (_logger.BeginScope(new Dictionary<string, object> { ["sql"] = sql }))
                        {
                            _logger.LogInformation(exception, "DB Retry {RetryCount} occurred after {timeSpan}", retryCount, calculatedWaitDuration);
                        }
                    });

            var setup = Policy.WrapAsync(waitAndRetryPolicyForError596, waitAndRetryPolicy);

            //using setter will use TryAdd behind the scenes and prevent duplicate key error
            _registry[StandardRetryPolicy] = setup;

            return setup;
        }

        /// <summary>
        /// This will get the policy that will retry 2 times when the following sql errors occur:
        /// Deadlock errors (Number = 1205) or
        /// Cannot continue the execution because the session is in the kill state (Number = 596)
        /// </summary>
        /// <returns></returns>
        public IAsyncPolicy GetStandardSaveQueryRetryPolicy()
        {
            if (_registry.ContainsKey(StandardSaveQueryRetryPolicy))
            {
                return _registry.Get<IAsyncPolicy>(StandardSaveQueryRetryPolicy);
            }

            var waitAndRetryPolicy = Policy
                .Handle<SqlException>(x => x.Number == 1205)
                .WaitAndRetryAsync(2,
                    retryAttempt => TimeSpan.FromMilliseconds(200 * retryAttempt),
                    onRetry: (exception, calculatedWaitDuration, retryCount, context) =>
                    {
                        var sql = context.ContainsKey("sql") ? context["sql"] : "";
                        using (_logger.BeginScope(new Dictionary<string, object> { ["sql"] = sql }))
                        {
                            _logger.LogInformation(exception, "DB Retry {RetryCount} occurred after {timeSpan}", retryCount, calculatedWaitDuration);
                        }
                    });

            // This error happens during the fail over and takes longer so have it in a different policy
            var waitAndRetryPolicyForError596 = Policy
                .Handle<SqlException>(x => x.Number == 596)
                .WaitAndRetryAsync(2,
                    retryAttempt => TimeSpan.FromSeconds(5 + retryAttempt),
                    onRetry: (exception, calculatedWaitDuration, retryCount, context) =>
                    {
                        var sql = context.ContainsKey("sql") ? context["sql"] : "";
                        using (_logger.BeginScope(new Dictionary<string, object> { ["sql"] = sql }))
                        {
                            _logger.LogInformation(exception, "DB Retry {RetryCount} occurred after {timeSpan}", retryCount, calculatedWaitDuration);
                        }
                    });

            var setup = Policy.WrapAsync(waitAndRetryPolicyForError596, waitAndRetryPolicy);

            //using setter will use TryAdd behind the scenes and prevent duplicate key error
            _registry[StandardSaveQueryRetryPolicy] = setup;

            return setup;
        }

        /// <summary>
        /// This will get the policy that will retry 2 times when the following sql errors occur:
        /// Unable to access availability database because the database replica is not in the PRIMARY or SECONDARY role (Number = 983)
        /// Target database is in an availability group and currently accessible for connections when the application intent is set to read only. (Number = 978)
        /// </summary>
        /// <returns></returns>
        public IAsyncPolicy GetOpenConnectionRetryPolicy()
        {
            if (_registry.ContainsKey(OpenConnectionRetryPolicy))
            {
                return _registry.Get<IAsyncPolicy>(OpenConnectionRetryPolicy);
            }

            var waitAndRetryPolicy = Policy
                .Handle<SqlException>(x => x.Number == 983 || x.Number == 978)
                .WaitAndRetryAsync(2,
                    retryAttempt => TimeSpan.FromSeconds(5 + retryAttempt),
                    onRetry: (exception, calculatedWaitDuration, retryCount, context) =>
                    {
                        _logger.LogInformation(exception, "Open Connection - DB Retry {RetryCount} occurred after {timeSpan}", retryCount, calculatedWaitDuration);
                    });

            //using setter will use TryAdd behind the scenes and prevent duplicate key error
            _registry[OpenConnectionRetryPolicy] = waitAndRetryPolicy;

            return waitAndRetryPolicy;
        }
    }
}
