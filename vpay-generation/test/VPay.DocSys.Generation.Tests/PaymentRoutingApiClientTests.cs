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
using VPay.DocSys.Generation.Common.Interfaces;
using Xunit;

namespace VPay.DocSys.Generation.Tests;

public class PaymentRoutingApiClientTests
{
    private readonly Mock<HttpMessageHandler> _httpClientSetup = new Mock<HttpMessageHandler>();
    private readonly ApiSettings _apiSettings;
    private readonly ServiceSettings _apiServiceSettings;

    private readonly IPaymentRoutingApiClient SUT;

    public PaymentRoutingApiClientTests()
    {
        _apiServiceSettings = new ServiceSettings
        {
            ServiceName = "PaymentRoutingApi",
            Host = "fake-host",
            Protocol = "http",
            EndPointFragments = new Dictionary<string, string>
            {
                { "GetOutbound835Document", "" },
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

        SUT = new PaymentRoutingApiClient(
          _apiSettings,
          httpClient,
          new NullLogger<PaymentRoutingApiClient>());
    }

    [Fact]
    public async Task ShouldReturnNullForAnyExceptionNotCaughtByPolly()
    {
        _httpClientSetup
            .SetupAnyRequest()
            .Throws<OperationCanceledException>()
            .Verifiable();

        var actual = await SUT.GetOutbound835Document(1, new System.Threading.CancellationToken());

        Assert.Null(actual);
    }

    [Theory]
    [MemberData(nameof(InvalidCtorParameters))]
    public void ShouldThrowExceptionOnInvalidCtorValues(Type exceptionType, ApiSettings apiSettings, HttpClient httpClient, ILogger<PaymentRoutingApiClient> logger)
    {
        Assert.Throws(exceptionType, () => new PaymentRoutingApiClient(apiSettings, httpClient, logger));
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
                        ServiceName = "PaymentRoutingApi",
                        Host = "fake-host",
                        Protocol = "http",
                        EndPointFragments = new Dictionary<string, string>
                        {
                            { "GetOutbound835Document", "" },
                        }
                    }
                }
            };
            var httpClient = new Mock<HttpMessageHandler>().CreateClient();
            var logger = new NullLogger<PaymentRoutingApiClient>();

            yield return new object[] { typeof(ArgumentNullException), null, httpClient, logger };
            yield return new object[] { typeof(ArgumentNullException), apiSettings, null, logger };
            yield return new object[] { typeof(ArgumentNullException), apiSettings, httpClient, null };
            yield return new object[] { typeof(ArgumentOutOfRangeException), new ApiSettings(), httpClient, logger };
        }
    }
}
