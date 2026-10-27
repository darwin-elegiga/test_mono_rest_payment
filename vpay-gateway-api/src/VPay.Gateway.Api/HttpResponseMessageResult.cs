using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace VPay.Gateway.Api;

/// <summary>
/// This class allows conversion of an <see cref="HttpResponseMessage"/>
/// into an <see cref="IActionResult" />.
/// </summary>
public class HttpResponseMessageResult : IActionResult
{
    private readonly HttpResponseMessage _responseMessage;

    public HttpResponseMessageResult(HttpResponseMessage responseMessage)
    {
        _responseMessage = responseMessage ?? throw new ArgumentNullException(nameof(responseMessage));
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;

        using (_responseMessage)
        {
            response.StatusCode = (int)_responseMessage.StatusCode;

            var responseFeature = context.HttpContext.Features.Get<IHttpResponseFeature>();
            if (responseFeature != null)
            {
                responseFeature.ReasonPhrase = _responseMessage.ReasonPhrase;
            }

            var responseHeaders = _responseMessage.Headers;

            // Ignore the Transfer-Encoding header if it is just "chunked".
            // We let the host decide about whether the response should be chunked or not.
            if (responseHeaders.TransferEncodingChunked == true &&
                responseHeaders.TransferEncoding.Count == 1)
            {
                responseHeaders.TransferEncoding.Clear();
            }

            foreach (var header in responseHeaders)
            {
                response.Headers.Append(header.Key, header.Value.ToArray());
            }

            if (_responseMessage.Content != null)
            {
                var contentHeaders = _responseMessage.Content.Headers;

                // Copy the response content headers only after ensuring they are complete.
                // We ask for Content-Length first because HttpContent lazily computes this
                // and only afterwards writes the value into the content headers.
                _ = contentHeaders.ContentLength;

                foreach (var header in contentHeaders)
                {
                    response.Headers.Append(header.Key, header.Value.ToArray());
                }

                await _responseMessage.Content.CopyToAsync(response.Body).ConfigureAwait(false);
            }
        }
    }
}
