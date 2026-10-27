using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Contrib.HttpClient;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Enums;
using Xunit;

namespace VPay.DocSys.Generation.Tests;

public class RemittancePartnerNetworkApiClientTests
{
    private readonly Mock<HttpMessageHandler> _httpClientSetup = new Mock<HttpMessageHandler>();
    private readonly ApiSettings _apiSettings;
    private readonly ServiceSettings _apiServiceSettings;

    private readonly IRemittancePartnerNetworkApiClient SUT;

    public RemittancePartnerNetworkApiClientTests()
    {
        _apiServiceSettings = new ServiceSettings
        {
            ServiceName = "RemittancePartnerNetworkApi",
            Host = "fake-host",
            Protocol = "http",
            EndPointFragments = new Dictionary<string, string>
                {
                    { "ValidateB2BRemittanceDocument", "" },
                    { "ValidateC2BRemittanceDocument", "" },
                    { "PostB2BRemittanceDocument", "" },
                    { "PostC2BRemittanceDocument", "" },
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

        SUT = new RemittancePartnerNetworkApiClient(
          _apiSettings,
          httpClient,
          new NullLogger<RemittancePartnerNetworkApiClient>());
    }

    [Fact]
    public async Task ShouldReturnFalseForAnyExceptionNotCaughtByPolly()
    {
        _httpClientSetup
            .SetupAnyRequest()
            .Throws<OperationCanceledException>()
            .Verifiable();

        var actual = await SUT.PostRemittanceDocument(RemittanceNetworkEnrollmentTypes.C2B, "TEST", default);

        Assert.True(actual.documentAccepted == false);
    }

    [Theory]
    [MemberData(nameof(InvalidCtorParameters))]
    public void ShouldThrowExceptionOnInvalidCtorValues(Type exceptionType, ApiSettings apiSettings, HttpClient httpClient, ILogger<RemittancePartnerNetworkApiClient> logger)
    {
        Assert.Throws(exceptionType, () => new RemittancePartnerNetworkApiClient(apiSettings, httpClient, logger));
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
                            ServiceName = "RemittancePartnerNetworkApi",
                            Host = "fake-host",
                            Protocol = "http",
                            EndPointFragments = new Dictionary<string, string>
                            {
                                { "ValidateB2BRemittanceDocument", "" },
                                { "ValidateC2BRemittanceDocument", "" },
                                { "PostB2BRemittanceDocument", "" },
                                { "PostC2BRemittanceDocument", "" },
                            }
                        }
                    }
            };
            var httpClient = new Mock<HttpMessageHandler>().CreateClient();
            var logger = new NullLogger<RemittancePartnerNetworkApiClient>();

            yield return new object[] { typeof(ArgumentNullException), null, httpClient, logger };
            yield return new object[] { typeof(ArgumentNullException), apiSettings, null, logger };
            yield return new object[] { typeof(ArgumentNullException), apiSettings, httpClient, null };
            yield return new object[] { typeof(ArgumentOutOfRangeException), new ApiSettings(), httpClient, logger };
        }
    }
}
