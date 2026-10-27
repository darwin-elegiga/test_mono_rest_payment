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
    public class GreenArrowApiClientTests
    {
        private readonly ServiceSettings _apiServiceSettings;
        private readonly Mock<HttpMessageHandler> _httpClientSetup = new Mock<HttpMessageHandler>();

        private readonly IGreenArrowApiClient SUT;

        public GreenArrowApiClientTests()
        {
            _apiServiceSettings = new ServiceSettings
            {
                ServiceName = "GreenArrowApi",
                Host = "fake-host",
                Protocol = "http",
                EndPointFragments = new Dictionary<string, string>
                {
                    { "SendEmail", "" },
                }
            };

            var apiSettings = new ApiSettings
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

            SUT = new GreenArrowApiClient(
                apiSettings,
                httpClient,
                new NullLogger<GreenArrowApiClient>());
        }

        [Fact]
        public async Task ShouldSendEmailAndGetOk()
        {
            var obj = new ReminderFaxEmailRequest(new ReminderFaxEmailSettings
            {
                FromEmail = "TEST FROM",
                FromName = "TEST FROM",
                ToEmail = "TEST TO",
                ToName = "TEST TO",
            }, DateTime.Now, DateTime.Now);

            await SUT.SendEmail(obj);

            _httpClientSetup.VerifyAnyRequest(Times.Once());
        }

        [Fact]
        public async Task ShouldThrowExceptionIfNotOkFromSendingEmail()
        {
            _httpClientSetup
                .SetupAnyRequest()
                .ReturnsResponse(HttpStatusCode.BadRequest)
                .Verifiable();

            var obj = new ReminderFaxEmailRequest(new ReminderFaxEmailSettings
            {
                FromEmail = "TEST FROM",
                FromName = "TEST FROM",
                ToEmail = "TEST TO",
                ToName = "TEST TO",
            }, DateTime.Now, DateTime.Now);

            await Assert.ThrowsAsync<Exception>(() => SUT.SendEmail(obj));

            _httpClientSetup.VerifyAnyRequest(Times.Once());
        }

        [Fact]
        public async Task ShouldThrowExceptionOnNullEmailRequest()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => SUT.SendEmail(null));
        }
    }
}
