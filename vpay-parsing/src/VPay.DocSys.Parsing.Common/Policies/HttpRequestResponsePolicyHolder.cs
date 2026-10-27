using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing
{
    public class HttpRequestResponsePolicyHolder
    {
        private readonly ILogger _logger;
        private readonly LogLevel _logLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestResponsePolicyHolder" /> class
        /// that will automatically retry on HTTP Request exceptions, timeout exceptions, or when
        /// receiving any of the transient <see cref="HttpStatusCode"/>s: 408 (Request Timeout)
        /// or 5XX (Server Errors) including 500 (Internal Server Error), 503 (Service Unavailable),
        /// 502 (Bad Gateway), and 504 (Gateway Timeout).
        /// </summary>
        /// <param name="retryCount">The number or retries to attempt after the first failure.</param>
        /// <param name="sleepDuration">The interval of time to wait before retrying again.</param>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="logLevel">The log level to use when logging retry attempts.</param>
        public HttpRequestResponsePolicyHolder(int retryCount, TimeSpan sleepDuration, ILogger logger, LogLevel logLevel = LogLevel.Information)
        {
            ArgumentHelpers.ThrowIf.IsLessThanZero(retryCount, nameof(retryCount));
            ArgumentHelpers.ThrowIf.IsNull(sleepDuration, nameof(sleepDuration));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _logger = logger;
            _logLevel = logLevel;

            // Retry policy retries on HTTP Request exceptions, timeout exceptions, or when
            // receiving any of the transient HTTP status codes.
            RetryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TaskCanceledException>()
                .Or<TimeoutException>()
                .Or<TimeoutRejectedException>()
                .WaitAndRetryAsync(retryCount, (r) => sleepDuration, OnRetry);
        }

        /// <summary>
        /// Gets the policy that should be used for retry operations.
        /// </summary>
        public IAsyncPolicy<HttpResponseMessage> RetryPolicy { get; }

        private void OnRetry(DelegateResult<HttpResponseMessage> response, TimeSpan sleepDuration, int retryCount, Context context)
        {
            var operation = context.OperationKey ?? "(unknown operation)";

            using (_logger.BeginScope(new Dictionary<string, object>()
            {
                { "RetryCount", retryCount},
                { "SleepDuration", sleepDuration },
                { "Operation", operation },
                { "PolicyHolder", this.GetType().Name },
            }))
            {
                _logger.LogException(_logLevel, response.Exception, $"Retry ({retryCount}): {operation}");
            }
        }
    }
}
