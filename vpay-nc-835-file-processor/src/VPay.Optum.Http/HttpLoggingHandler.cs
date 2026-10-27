using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VPay.Optum.Http
{
    /// <summary>
    /// An instance of a <see cref="DelegatingHandler"/> that can be used
    /// to log the request and response through an http client.
    /// </summary>
    /// <remarks>
    /// This can be very useful to view the request and response information.
    /// When using something like Refit, it's very useful to log the information
    /// to see exactly what is being sent and received.
    /// </remarks>
    public class HttpLoggingHandler : DelegatingHandler
    {
        private readonly ILogger<HttpLoggingHandler> _logger;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly string[] _types = { "html", "text", "xml", "json", "txt", "x-www-form-urlencoded" };

        public HttpLoggingHandler(ILogger<HttpLoggingHandler> logger, IHostEnvironment hostEnvironment)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!_hostEnvironment.IsDevelopment() && !_hostEnvironment.IsEnvironment("LocalDevelopment"))
            {
                // NOTE: We only want to log all this information if the Debug log level is enabled
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }

            var id = Guid.NewGuid().ToString();
            var response = await LogRequest(id, request, cancellationToken).ConfigureAwait(false);

            await LogResponse(id, request, response).ConfigureAwait(false);
            return response;
        }

        private async Task<HttpResponseMessage> LogRequest(string id, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var msg = $"[{id} - Request]";

            _logger.LogDebug("{MessageId}========Start==========", msg);
            _logger.LogDebug(
                "{MessageId} {Method} {UriPathAndQuery} {UriScheme}/{Version}",
                msg,
                request.Method,
                request.RequestUri?.PathAndQuery,
                request.RequestUri?.Scheme,
                request.Version);

            _logger.LogDebug("{MessageId} Host: {UriScheme}://{UriHost}",
                msg,
                request.RequestUri?.Scheme,
                request.RequestUri?.Host);

            LogHeaders(msg, request.Headers);

            if (request.Content != null)
            {
                LogHeaders(msg, request.Content.Headers);

                if (request.Content is StringContent
                    || IsTextBasedContentType(request.Headers)
                    || IsTextBasedContentType(request.Content.Headers))
                {
                    var result = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                    _logger.LogDebug("{MessageId} Content:", msg);
                    _logger.LogDebug("{MessageId} {Request}", msg, result);
                }
            }

            var stopwatch = Stopwatch.StartNew();

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            stopwatch.Stop();

            _logger.LogDebug("{MessageId} Duration: {Duration}", msg, stopwatch.Elapsed);
            _logger.LogDebug("{MessageId}==========End==========", msg);
            return response;
        }

        private async Task LogResponse(string id, HttpRequestMessage request, HttpResponseMessage response)
        {
            var responseMessage = $"[{id} - Response]";
            _logger.LogDebug("{MessageId}=========Start=========", responseMessage);

            _logger.LogDebug(
                "{MessageId} {UriScheme}/{Version} {StatusCode} {ReasonPhrase}",
                responseMessage,
                request.RequestUri?.Scheme.ToUpper(),
                response.Version,
                (int)response.StatusCode,
                response.ReasonPhrase);

            LogHeaders(responseMessage, response.Headers);

            if (response.Content != null)
            {
                LogHeaders(responseMessage, response.Content.Headers);

                if (response.Content is StringContent
                    || IsTextBasedContentType(response.Headers)
                    || IsTextBasedContentType(response.Content.Headers))
                {
                    var stopwatch = Stopwatch.StartNew();
                    var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    stopwatch.Stop();

                    _logger.LogDebug("{MessageId} Content:", responseMessage);
                    _logger.LogDebug("{MessageId} {Response}", responseMessage, result);
                    _logger.LogDebug("{MessageId} Duration: {Duration}", responseMessage, stopwatch.Elapsed);
                }
            }

            _logger.LogDebug("{MessageId}==========End==========", responseMessage);
        }

        private void LogHeaders(string message, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            foreach (var header in headers)
            {
                _logger.LogDebug("{MessageId} {HeaderKey}: {HeaderValue}", message, header.Key, string.Join(", ", header.Value));
            }
        }

        private bool IsTextBasedContentType(HttpHeaders headers)
        {
            if (!headers.TryGetValues("Content-Type", out IEnumerable<string>? values))
            {
                return false;
            }

            var header = string.Join(" ", values).ToLowerInvariant();
            return _types.Any(t => header.Contains(t));
        }
    }
}
