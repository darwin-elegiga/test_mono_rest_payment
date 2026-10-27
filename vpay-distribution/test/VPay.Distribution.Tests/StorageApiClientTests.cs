
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;
using Moq.Contrib.HttpClient;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using VPay.Distribution.Common;
using VPay.DocSys.Common;
using VPay.DocSys.Storage;

using Xunit;

namespace VPay.Distribution.Tests
{
    public class StorageApiClientTests
    {
        private readonly Mock<HttpMessageHandler> _httpClientSetup = new Mock<HttpMessageHandler>();
        private readonly HttpRequestPolicyHolder _httpPolicyHolder = new HttpRequestPolicyHolder(new NullLogger<HttpRequestPolicyHolder>());

        private readonly IStorageApiClient SUT;

        private readonly string baseUri = "http://localhost";

        public StorageApiClientTests()
        {
            var httpClient = _httpClientSetup.CreateClient();
            httpClient.BaseAddress = new Uri(baseUri);

            var result = new[] { new ArchiveFile
            {
                PathName =string.Empty,
                FileName=string.Empty,
                ArchiveDetails = new[] { new ArchiveDetail() }
            } };

            _httpClientSetup
                .SetupAnyRequest()
                .ReturnsResponse(JsonConvert.SerializeObject(result, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                }))
                .Verifiable();

            SUT = new StorageApiClient(
                httpClient,
                _httpPolicyHolder,
                new NullLogger<StorageApiClient>());
        }

        [Theory]
        [MemberData(nameof(InvalidCtorValues))]
        public void ShouldThrowExceptionOnInvalidCtor(Type exception, HttpClient httpClient, HttpRequestPolicyHolder httpRequestPolicyHolder, ILogger<StorageApiClient> logger)
        {
            Assert.Throws(exception, () => new StorageApiClient(httpClient, httpRequestPolicyHolder, logger));
        }

        [Fact]
        public async Task WhenGetting()
        {
            await SUT.GetArchivesForTransactionId(0, new CancellationToken());

            _httpClientSetup.VerifyAnyRequest(Times.Once());
        }

        public static IEnumerable<object[]> InvalidCtorValues
        {
            get
            {
                var client = Mock.Of<HttpClient>();
                var requestPolicyHolder = new HttpRequestPolicyHolder(new NullLogger<HttpRequestPolicyHolder>());
                var logger = new NullLogger<StorageApiClient>();

                yield return new object[] { typeof(ArgumentNullException), null, requestPolicyHolder, logger };
                yield return new object[] { typeof(ArgumentNullException), client, null, logger };
                yield return new object[] { typeof(ArgumentNullException), client, requestPolicyHolder, null };
            }
        }
    }
}
