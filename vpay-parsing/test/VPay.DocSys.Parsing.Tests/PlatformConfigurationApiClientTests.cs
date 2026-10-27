using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Models;
using Xunit;

namespace VPay.DocSys.Parsing.Tests
{
    public class PlatformConfigurationApiClientTests
    {
        private readonly Mock<HttpMessageHandler> _httpClientSetup = new Mock<HttpMessageHandler>();
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _apiSettings;
        private readonly ServiceSettings _apiServiceSettings;
        private readonly IPlatformConfigurationApiClient _apiService;
        private readonly ILogger<PlatformConfigurationApiClient> _nullLogger = new NullLogger<PlatformConfigurationApiClient>();

        #region Test Data

        private const string _content = @"{
          'clientCode': 'TEST',
          'originCode': 'OOO',
          'originName': 'Test Medical',
          'billingEntities': [
            {
              'billingEntityCode': 'TEST000001',
              'fields': [
                {
                  'fieldName': '00',
                  'fieldValue': '42',
                  'fieldOrder': 0,
                  'effectiveFromDate': '2021-02-05'
                },
                {
                  'fieldName': 'PayType',
                  'fieldValue': '*',
                  'fieldOrder': 1,
                  'effectiveFromDate': '2021-02-05'
                },
                {
                  'fieldName': 'TPA',
                  'fieldValue': 'TEST',
                  'fieldOrder': 2,
                  'effectiveFromDate': '2021-02-05'
                }
              ]
            }
          ],
          'effectiveFromDate': '2021-02-05'
        }";

        private const string _clientCode = "TEST";
        private const string _beCode = "TEST000001";
        private static readonly ICollection<string> _beCodes = new List<string> { _beCode };
        private static readonly IEnumerable<OtbBillingEntityDetailDto> _billingEntities = new List<OtbBillingEntityDetailDto>
        {
            new OtbBillingEntityDetailDto
            {
                BillingEntityCode = _beCode,
                Fields = new List<OtbBillingEntityFieldDto>
                {
                    new OtbBillingEntityFieldDto
                    {
                        FieldName = "00",
                        FieldValue = "42",
                        FieldOrder = 0,
                        EffectiveFromDate = DateTime.Parse("2021-02-05"),
                    },
                    new OtbBillingEntityFieldDto
                    {
                        FieldName = "PayType",
                        FieldValue = "*",
                        FieldOrder = 1,
                        EffectiveFromDate = DateTime.Parse("2021-02-05"),
                    },
                    new OtbBillingEntityFieldDto
                    {
                        FieldName = "TPA",
                        FieldValue = _clientCode,
                        FieldOrder = 2,
                        EffectiveFromDate = DateTime.Parse("2021-02-05"),
                    }
                }
            }
        };

        #endregion

        public PlatformConfigurationApiClientTests()
        {
            _apiServiceSettings = new ServiceSettings
            {
                ServiceName = "PlatformConfigurationApi",
                Host = "fake-host",
                Protocol = "http",
                EndPointFragments = new Dictionary<string, string>
                {
                    { "GetOtbDetails", "" },
                }
            };

            _apiSettings = new ApiSettings
            {
                ServiceSettings = new List<ServiceSettings>
                {
                    _apiServiceSettings
                }
            };

            _httpClientSetup.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(_content)
                })
                .Verifiable();

            _httpClient = new HttpClient(_httpClientSetup.Object)
            {
                BaseAddress = _apiServiceSettings.GetUri()
            };

            _apiService = new PlatformConfigurationApiClient(_apiSettings, _httpClient, _nullLogger);
        }

        [Fact]
        public async Task ShouldThrowExceptionForAnyExceptionNotCaughtByPolly()
        {
            _httpClientSetup.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Throws<OperationCanceledException>()
                .Verifiable();

            Func<Task> result = async () =>
            {
                await _apiService.GetOtbClientBillingEntities(_clientCode, _beCodes).ConfigureAwait(false);
            };

            await result.Should().ThrowAsync<Exception>();
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
                                { "GetOtbDetails", "" },
                            }
                        }
                    }
                };
                var httpClient = new HttpClient(new Mock<HttpMessageHandler>().Object);
                var logger = new NullLogger<PlatformConfigurationApiClient>();

                yield return new object[] { typeof(ArgumentNullException), null, httpClient, logger };
                yield return new object[] { typeof(ArgumentNullException), apiSettings, null, logger };
                yield return new object[] { typeof(ArgumentNullException), apiSettings, httpClient, null };
                yield return new object[] { typeof(ArgumentOutOfRangeException), new ApiSettings(), httpClient, logger };
            }
        }

        private IPlatformConfigurationApiClient CreateApiService(Mock<HttpMessageHandler> httpClientSetup)
        {
            var httpClient = new HttpClient(httpClientSetup.Object)
            {
                BaseAddress = _apiServiceSettings.GetUri()
            };

            var apiService = new PlatformConfigurationApiClient(_apiSettings, httpClient, _nullLogger);

            return apiService;
        }

        [Fact]
        public async Task ShouldReturnResultWhenRequestsIsValid()
        {
            var httpClientSetup = new Mock<HttpMessageHandler>();

            httpClientSetup.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(_content)
                })
                .Verifiable();

            var apiService = CreateApiService(httpClientSetup);

            var result = await apiService.GetOtbClientBillingEntities(_clientCode, _beCodes).ConfigureAwait(false);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.ClientCode.Should().NotBeNullOrEmpty().And.Be(_clientCode);
                result.BillingEntities.Should().NotBeNullOrEmpty().And.BeEquivalentTo(_billingEntities);
            }
        }

        [Fact]
        public async Task ShouldRetryWhenReceivingTimeoutOnce()
        {
            var httpClientSetup = new Mock<HttpMessageHandler>();

            httpClientSetup.Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(_content)
                });

            var apiService = CreateApiService(httpClientSetup);

            var result = await apiService.GetOtbClientBillingEntities(_clientCode, _beCodes).ConfigureAwait(false);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.ClientCode.Should().NotBeNullOrEmpty().And.Be(_clientCode);
                result.BillingEntities.Should().NotBeNullOrEmpty().And.BeEquivalentTo(_billingEntities);

                // The API should have been called exactly 2 times:
                // a) first time: received a Timeout exception.
                // b) second time: received the correct result.            
                httpClientSetup.Protected()
                    .Verify("SendAsync", Times.Exactly(2), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
            }
        }

        [Fact]
        public async Task ShouldRetryWhenReceivingTimeoutTwice()
        {
            var httpClientSetup = new Mock<HttpMessageHandler>();

            httpClientSetup.Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException())
                .ThrowsAsync(new TaskCanceledException())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(_content)
                });

            var apiService = CreateApiService(httpClientSetup);

            var result = await apiService.GetOtbClientBillingEntities(_clientCode, _beCodes).ConfigureAwait(false);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.ClientCode.Should().NotBeNullOrEmpty().And.Be(_clientCode);
                result.BillingEntities.Should().NotBeNullOrEmpty().And.BeEquivalentTo(_billingEntities);

                // The API should have been called exactly 3 times:
                // a) first and second time: received a Timeout exception.
                // b) third time: received the correct result.            
                httpClientSetup.Protected()
                    .Verify("SendAsync", Times.Exactly(3), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
            }
        }

        [Fact]
        public async Task ShouldThrowExceptionWhenReceivingTimeoutThrice()
        {
            var httpClientSetup = new Mock<HttpMessageHandler>();

            httpClientSetup.Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException())
                .ThrowsAsync(new TaskCanceledException())
                .ThrowsAsync(new TaskCanceledException());

            var apiService = CreateApiService(httpClientSetup);

            Func<Task> result = async () =>
            {
                await apiService.GetOtbClientBillingEntities(_clientCode, _beCodes).ConfigureAwait(false);
            };

            using (new AssertionScope())
            {
                //result.Should().BeNull();
                await result.Should().ThrowAsync<Exception>();

                // The API should have been called exactly 3 times:
                // a) first, second time, and third time: received a Timeout exception.
                httpClientSetup.Protected()
                    .Verify("SendAsync", Times.Exactly(3), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
            }
        }

        [Fact]
        public async Task ShouldThrowExceptionWhenReceivingBadRequestError()
        {
            var httpClientSetup = new Mock<HttpMessageHandler>();

            httpClientSetup.Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(_content)
                });

            var apiService = CreateApiService(httpClientSetup);

            Func<Task> result = async () =>
            {
                await apiService.GetOtbClientBillingEntities(_clientCode, _beCodes).ConfigureAwait(false);
            };

            await result.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task ShouldRetryWhenReceivingBadGatewayOnce()
        {
            var httpClientSetup = new Mock<HttpMessageHandler>();

            httpClientSetup.Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadGateway })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(_content)
                });

            var apiService = CreateApiService(httpClientSetup);

            var result = await apiService.GetOtbClientBillingEntities(_clientCode, _beCodes).ConfigureAwait(false);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.ClientCode.Should().NotBeNullOrEmpty().And.Be(_clientCode);
                result.BillingEntities.Should().NotBeNullOrEmpty().And.BeEquivalentTo(_billingEntities);

                // The API should have been called exactly 2 times:
                // a) first time: received a Bad Gateway response.
                // b) second time: received the correct result.            
                httpClientSetup.Protected()
                    .Verify("SendAsync", Times.Exactly(2), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
            }
        }

        [Fact]
        public async Task ShouldRetryWhenReceivingBadGatewayTwice()
        {
            var httpClientSetup = new Mock<HttpMessageHandler>();

            httpClientSetup.Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadGateway })
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadGateway })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(_content)
                });

            var apiService = CreateApiService(httpClientSetup);

            var result = await apiService.GetOtbClientBillingEntities(_clientCode, _beCodes).ConfigureAwait(false);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.ClientCode.Should().NotBeNullOrEmpty().And.Be(_clientCode);
                result.BillingEntities.Should().NotBeNullOrEmpty().And.BeEquivalentTo(_billingEntities);

                // The API should have been called exactly 3 times:
                // a) first and second time: received a Bad Gateway response.
                // b) third time: received the correct result.            
                httpClientSetup.Protected()
                    .Verify("SendAsync", Times.Exactly(3), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
            }
        }

        [Fact]
        public async Task ShouldThrowExceptionWhenReceivingBadGatewayThrice()
        {
            var httpClientSetup = new Mock<HttpMessageHandler>();

            httpClientSetup.Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadGateway })
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadGateway })
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadGateway });

            var apiService = CreateApiService(httpClientSetup);

            Func<Task> result = async () =>
            {
                await apiService.GetOtbClientBillingEntities(_clientCode, _beCodes).ConfigureAwait(false);
            };

            using (new AssertionScope())
            {
                await result.Should().ThrowAsync<Exception>();

                // The API should have been called exactly 3 times:
                // a) first, second time, and third time: received a Bad Gateway response.
                httpClientSetup.Protected()
                    .Verify("SendAsync", Times.Exactly(3), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
            }
        }
    }
}
