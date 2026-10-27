using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;

namespace VPay.DocSys.Generation
{
    public class StaticFileApiClient : ClientApiBase, IStaticFileApiClient
    {
        public const string ServiceName = "StaticFileApi";

        private readonly HttpRequestPolicyHolder _policies;

        public StaticFileApiClient(
            IHttpClientFactory httpClientFactory,
            ILogger<StaticFileApiClient> logger)
            : base(httpClientFactory, logger, ServiceName)
        {
            _policies = new HttpRequestPolicyHolder(logger);
        }

        public Task<Stream> GetContentStream(string contentFilePath)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(contentFilePath, nameof(contentFilePath));

            return _policies.HttpGetPolicy.ExecuteAsync(() =>
            {
                return GetStream(contentFilePath);

            }, this.GetUri(contentFilePath).ToString());
        }
    }
}
