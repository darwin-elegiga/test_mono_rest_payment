using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;
using Moq.Contrib.HttpClient;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;

using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class PlatformConfigurationApiClientTests
    {
        private readonly Mock<HttpMessageHandler> _httpClientSetup = new Mock<HttpMessageHandler>();
        private readonly ApiSettings _apiSettings;
        private readonly ServiceSettings _apiServiceSettings;

        private readonly IPlatformConfigurationApiClient SUT;

        public PlatformConfigurationApiClientTests()
        {
            _apiServiceSettings = new ServiceSettings
            {
                ServiceName = "PlatformConfigurationApi",
                Host = "fake-host",
                Protocol = "http",
                EndPointFragments = new Dictionary<string, string>
                {
                    { "GetBillingEntityDetails", "" },
                }
            };

            _apiSettings = new ApiSettings
            {
                ServiceSettings = new List<ServiceSettings>
                {
                    _apiServiceSettings
                }
            };

            var httpClient = _httpClientSetup.CreateClient();
            httpClient.BaseAddress = _apiServiceSettings.GetUri();

            _httpClientSetup
                .SetupAnyRequest()
                .ReturnsResponse(HttpStatusCode.OK)
                .Verifiable();

            SUT = new PlatformConfigurationApiClient(
              _apiSettings,
              httpClient,
              new NullLogger<PlatformConfigurationApiClient>());
        }

        [Fact]
        public async Task ShouldReturnNullForAnyExceptionNotCaughtByPolly()
        {
            _httpClientSetup
                .SetupAnyRequest()
                .Throws<OperationCanceledException>()
                .Verifiable();

            var actual = await SUT.GetOtbDetail("TEST", "TEST");

            Assert.Null(actual);
        }

        [Theory]
        [MemberData(nameof(InvalidCtorParameters))]
        public void ShouldThrowExceptionOnInvalidCtorValues(Type exceptionType, ApiSettings apiSettings, HttpClient httpClient, ILogger<PlatformConfigurationApiClient> logger)
        {
            Assert.Throws(exceptionType, () => new PlatformConfigurationApiClient(apiSettings, httpClient, logger));
        }

        public static IEnumerable<object[]> InvalidCtorParameters
        {
            get
            {
                var apiSettings = new ApiSettings
                {
                    ServiceSettings = new List<ServiceSettings>{
                        new ServiceSettings
                        {
                            ServiceName = "PlatformConfigurationApi",
                            Host = "fake-host",
                            Protocol = "http",
                            EndPointFragments = new Dictionary<string, string>
                            {
                                { "GetBillingEntityDetails", "" },
                            }
                        }
                    }
                };
                var httpClient = new Mock<HttpMessageHandler>().CreateClient();
                var logger = new NullLogger<PlatformConfigurationApiClient>();

                yield return new object[] { typeof(ArgumentNullException), null, httpClient, logger };
                yield return new object[] { typeof(ArgumentNullException), apiSettings, null, logger };
                yield return new object[] { typeof(ArgumentNullException), apiSettings, httpClient, null };
                yield return new object[] { typeof(ArgumentOutOfRangeException), new ApiSettings(), httpClient, logger };
            }
        }
    }
}
