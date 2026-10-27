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
    public class ParsingApiClientTests
    {
        private readonly Mock<HttpMessageHandler> _httpClientSetup = new Mock<HttpMessageHandler>();
        private readonly ApiSettings _apiSettings;
        private readonly ILogger<ParsingApiClient> _logger = new NullLogger<ParsingApiClient>();

        private readonly IParsingApiClient SUT;

        public ParsingApiClientTests()
        {
            _apiSettings = new ApiSettings
            {
                ServiceSettings = new List<ServiceSettings>
                {
                    new ServiceSettings
                    {
                        ServiceName = "ParsingApi",
                        Host = "fake-host",
                        Protocol = "http",
                        EndPointFragments = new Dictionary<string, string>
                        {
                            { "XmlConfig", "" },
                        }
                    }
                }
            };

            var httpClient = _httpClientSetup.CreateClient();
            httpClient.BaseAddress = _apiSettings.ServiceSettings[0].GetUri();

            _httpClientSetup
                .SetupAnyRequest()
                .ReturnsResponse(HttpStatusCode.OK)
                .Verifiable();

            SUT = new ParsingApiClient(_apiSettings, httpClient, _logger);
        }

        [Theory]
        [MemberData(nameof(InvalidCtorParameters))]
        public void ShouldThrowExceptionOnInvalidCtorValues(Type exceptionType, ApiSettings apiSettings, HttpClient httpClient, ILogger<ParsingApiClient> logger)
        {
            Assert.Throws(exceptionType, () => new ParsingApiClient(apiSettings, httpClient, logger));
        }

        public static IEnumerable<object[]> InvalidCtorParameters
        {
            get
            {
                var apiSettings = new ApiSettings
                {
                    ServiceSettings = new List<ServiceSettings>
                    {
                        new ServiceSettings
                        {
                            ServiceName = "ParsingApi",
                            Host = "fake-host",
                            Protocol = "http",
                            EndPointFragments = new Dictionary<string, string>
                            {
                                { "XmlConfig", "" },
                            }
                        }
                    }
                };
                var httpClient = new Mock<HttpMessageHandler>().CreateClient();
                var logger = new NullLogger<ParsingApiClient>();

                yield return new object[] { typeof(ArgumentNullException), null, httpClient, logger };
                yield return new object[] { typeof(ArgumentNullException), apiSettings, null, logger };
                yield return new object[] { typeof(ArgumentNullException), apiSettings, httpClient, null };
                yield return new object[] { typeof(ArgumentOutOfRangeException), new ApiSettings(), httpClient, logger };
            }
        }
    }
}
