using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.NoOp;
using Polly.Retry;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing
{
    public class HttpRequestPolicyHolder
    {
        public static readonly int NumberOfRetries = 2;
        public static readonly TimeSpan SleepDuration = TimeSpan.FromSeconds(3);

        private readonly ILogger _logger;
        private readonly LogLevel _logLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestPolicyHolder" /> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="logLevel">The log level to use when logging retry attempts.</param>
        public HttpRequestPolicyHolder(ILogger logger, LogLevel logLevel = LogLevel.Information)
        {
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _logger = logger;
            _logLevel = logLevel;

            // read-only policy retries on request exceptions, i/o exceptions AND time-out exceptions
            this.ReadPolicy = Policy
                .Handle<HttpRequestException>()     // handle http request exceptions
                    .Or<TaskCanceledException>()    // handle timeout exceptions
                    .Or<IOException>()               // handle i/o exceptions
                .WaitAndRetryAsync(
                    NumberOfRetries,
                    (r) => SleepDuration,
                    this.OnRetry);

            // write policy retries ONLY on request exceptions but no retry for i/o or time-out exceptions since the operation probably started if the request was successfully initiated
            this.WritePolicy = Policy
                .Handle<HttpRequestException>()     // handle http request exceptions
                .WaitAndRetryAsync(
                    NumberOfRetries,
                    (r) => SleepDuration,
                    this.OnRetry);

            // no-op policy does not retry
            this.NoOpPolicy = Policy.NoOpAsync();
        }

        /// <summary>
        /// Gets the policy that should be used for read-only operations.
        /// </summary>
        public IAsyncPolicy ReadPolicy { get; }

        /// <summary>
        /// Gets the policy that should be used for write operations.
        /// </summary>
        public IAsyncPolicy WritePolicy { get; }

        /// <summary>
        /// Gets the policy that does not implement retries.
        /// </summary>
        public IAsyncPolicy NoOpPolicy { get; }

        /// <summary>
        /// Gets the policy to use for http GET requests.
        /// </summary>
        public IAsyncPolicy HttpGetPolicy
        {
            get => this.ReadPolicy; // use the read policy for GET operations
        }

        /// <summary>
        /// Gets the policy to use for http POST requests.
        /// </summary>
        public IAsyncPolicy HttpPostPolicy
        {
            get => this.WritePolicy; // use the write policy for POST operations
        }

        /// <summary>
        /// Gets the policy to use for http PUT requests.
        /// </summary>
        public IAsyncPolicy HttpPutPolicy
        {
            get => this.WritePolicy; // use the write policy for PUT operations
        }

        /// <summary>
        /// Gets the policy to use for http PATCH requests.
        /// </summary>
        public IAsyncPolicy HttpPatchPolicy
        {
            get => this.WritePolicy; // use the write policy for PATCH operations
        }

        /// <summary>
        /// Gets the policy to use for http DELETE requests.
        /// </summary>
        public IAsyncPolicy HttpDeletePolicy
        {
            get => this.WritePolicy; // use the write policy for DELETE operations
        }

        private void OnRetry(Exception exception, TimeSpan sleepDuration, int retryCount, Context context)
        {
            var operation = context.OperationKey ?? "(unknown operation)";

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
