using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;

namespace VPay.DocSys.Generation
{
    public class FopApiClient : ClientApiBase, IFopApiClient
    {
        public const string ServiceName = "FOP";
        private const string EndPoint = "/";

        private readonly HttpRequestPolicyHolder _policies;

        public FopApiClient(
            IHttpClientFactory httpClientFactory,
            ILogger<FopApiClient> logger)
            : base(httpClientFactory, logger, ServiceName)
        {
            _policies = new HttpRequestPolicyHolder(logger);
        }

        public Task<Stream> PostFile(string xmlContent)
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("fodata", xmlContent),
            });

                return _policies.HttpPostPolicy.ExecuteAsync(() =>
                {
                    return PostStream(EndPoint, formContent);

                }, this.GetUri(EndPoint).ToString());

        }
    }
}
