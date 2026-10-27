using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.NoOp;
using Polly.Registry;
using Polly.Retry;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing
{
    public class SqlServerPolicyHolder
    {
        public static readonly int NumberOfRetries = 2;
        public static readonly TimeSpan SleepDuration = TimeSpan.FromSeconds(3);

        #region [-- Known Error Numbers & Messages --]

        private const int ConnectionFailureErrorNumber = 0;
        private const string ConnectionFailureErrorMessage = "A network-related or instance-specific error occurred while establishing a connection to SQL Server.";

        private const int ConnectionTimeoutErrorNumber = -2;
        private const string ConnectionTimeoutErrorMessage = "Connection Timeout Expired.";

        private const int CommandTimeoutErrorNumber = -2;
        private const string CommandTimeoutErrorMessage = "Timeout expired.";

        #endregion

        private readonly ILogger _logger;
        private readonly LogLevel _logLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerPolicyHolder"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="logLevel">The log level to use when logging retry attempts.</param>
        public SqlServerPolicyHolder(ILogger logger, LogLevel logLevel = LogLevel.Information)
        {
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _logger = logger;
            _logLevel = logLevel;

            // read-only policy retries on connection failures AND command time-out exceptions
            this.ReadPolicy = Policy
                .Handle<SqlException>(ex => ex.Message.StartsWith(ConnectionFailureErrorMessage) && ex.Number == ConnectionFailureErrorNumber)
                    .Or<SqlException>(ex => ex.Message.StartsWith(ConnectionTimeoutErrorMessage) && ex.Number == ConnectionTimeoutErrorNumber)
                    .Or<SqlException>(ex => ex.Message.StartsWith(CommandTimeoutErrorMessage) && ex.Number == CommandTimeoutErrorNumber)
                .WaitAndRetry(
                    NumberOfRetries,
                    (r) => SleepDuration,
                    this.OnRetry);

            // write policy retries ONLY on connection failures but no retry for command time-out exceptions since the operation probably started if the connection was made and operation  was successfully initiated
            this.WritePolicy = Policy
                .Handle<SqlException>(ex => ex.Message.StartsWith(ConnectionFailureErrorMessage) && ex.Number == ConnectionFailureErrorNumber)
                    .Or<SqlException>(ex => ex.Message.StartsWith(ConnectionTimeoutErrorMessage) && ex.Number == ConnectionTimeoutErrorNumber)
                .WaitAndRetry(
                    NumberOfRetries,
                    (r) => SleepDuration,
                    this.OnRetry);

            this.NoOpPolicy = Policy.NoOp();
        }

        /// <summary>
        /// Gets the policy that should be used for read-only operations.
        /// </summary>
        public ISyncPolicy ReadPolicy { get; }

        /// <summary>
        /// Gets the policy that should be used for write operations.
        /// </summary>
        public ISyncPolicy WritePolicy { get; }

        /// <summary>
        /// Gets the policy that does not implement retries.
        /// </summary>
        public ISyncPolicy NoOpPolicy { get; }

        /// <summary>
        /// Gets the policy to use for SELECT operations.
        /// </summary>
        public ISyncPolicy SelectPolicy
        {
            get => this.ReadPolicy;
        }

        /// <summary>
        /// Gets the policy to use for INSERT operations.
        /// </summary>
        public ISyncPolicy InsertPolicy
        {
            get => this.WritePolicy;
        }

        /// <summary>
        /// Gets the policy to use for UPDATE operations.
        /// </summary>
        public ISyncPolicy UpdatePolicy
        {
            get => this.WritePolicy;
        }

        /// <summary>
        /// Gets the policy to use for DELETE operations.
        /// </summary>
        public ISyncPolicy DeletePolicy
        {
            get => this.WritePolicy;
        }

        private void OnRetry(Exception exception, TimeSpan sleepDuration, int retryCount, Context context)
        {
            var operation = context?.OperationKey ?? "(unknown operation)";

            using(_logger.BeginScope(new Dictionary<string, object>()
            {
                { "RetryCount", retryCount},
                { "SleepDuration", sleepDuration },
                { "Operation", operation },
                { "PolicyHolder", this.GetType().Name },
            }))
            {
                _logger.LogException(_logLevel, exception, $"Retry ({retryCount}): {operation}");
            }
        }
    }
}
