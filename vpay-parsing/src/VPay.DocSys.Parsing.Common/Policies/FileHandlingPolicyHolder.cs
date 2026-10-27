using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;

namespace VPay.DocSys.Parsing
{
    public class FileHandlingPolicyHolder : IFileHandlingPolicyHolder
    {
        public static readonly int NumberOfRetries = 2;
        public static readonly TimeSpan SleepDuration = TimeSpan.FromSeconds(3);

        #region [-- Known Error Numbers & Messages --]

        private const string DirectoryNotEmptyErrorMessage = "Directory not empty";


        #endregion

        private readonly ILogger _logger;
        private readonly LogLevel _logLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileHandlingPolicyHolder"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="logLevel">The log level to use when logging retry attempts.</param>
        public FileHandlingPolicyHolder(ILogger logger, LogLevel logLevel = LogLevel.Information)
        {
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _logger = logger;
            _logLevel = logLevel;

            // delete policy retries on non-empty errors
            this.RemoveDirectoryPolicy = Policy
                .Handle<IOException>(ex => ex.Message.StartsWith(DirectoryNotEmptyErrorMessage))
                .WaitAndRetry(
                    NumberOfRetries,
                    (r) => SleepDuration,
                    this.OnRetry);

            this.NoOpPolicy = Policy.NoOp();
        }

        /// <summary>
        /// Gets the policy that should be used for delete directory operations.
        /// </summary>
        public ISyncPolicy RemoveDirectoryPolicy { get; }


        /// <summary>
        /// Gets the policy that does not implement retries.
        /// </summary>
        public ISyncPolicy NoOpPolicy { get; }

        /// <summary>
        /// Gets the policy to use for Delete/Remove Directory operations.
        /// </summary>
        public ISyncPolicy DeletePolicy => RemoveDirectoryPolicy;

        private void OnRetry(Exception exception, TimeSpan sleepDuration, int retryCount, Context context)
        {
            var operation = context?.OperationKey ?? "(directory deletion operation )";

            using (_logger.BeginScope(new Dictionary<string, object>()
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
