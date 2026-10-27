using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace VPay.Optum.Http
{
    public class RemoveDoubleSlashDelegatingHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri != null)
            {
                var builder = new UriBuilder(request.RequestUri);
                builder.Path = builder.Path.Replace("//", "/");
                request.RequestUri = builder.Uri;
            }
            return base.SendAsync(request, cancellationToken);
        }
    }
}
