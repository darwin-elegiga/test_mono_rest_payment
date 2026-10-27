using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Models;
using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public static class EftFaxPdfCreatorTests
    {
        public static class WhenCreatePdfStream
        {
            public class ShouldThrowError
            {
                private Mock<IStaticFileApiClient> _staticFileApiClient = new Mock<IStaticFileApiClient>(MockBehavior.Strict);
                private Mock<IFopApiClient> _fopApiClient = new Mock<IFopApiClient>(MockBehavior.Strict);
                private Mock<EftFaxFopXsl> _eftFaxFopXml = new Mock<EftFaxFopXsl>(MockBehavior.Strict);

                private ApiSettings _apiSettings = new ApiSettings
                {
                    ServiceSettings = new List<ServiceSettings>
                    {
                        new ServiceSettings
                        {
                            ServiceName = StaticFileApiClient.ServiceName,
                            Host = "test.com",
                            Port = 80,
                            Protocol = "http",
                        },
                    },
                };

                private Mock<ILogger<EftFaxPdfCreator>> _logger = new Mock<ILogger<EftFaxPdfCreator>>(MockBehavior.Loose);

                private IEftFaxPdfCreator SUT => new EftFaxPdfCreator(
                    _staticFileApiClient.Object,
                    _fopApiClient.Object,
                    _eftFaxFopXml.Object,
                    _apiSettings,
                    _logger.Object);

                [Fact]
                public async Task IsTransactionPeriodElementAdded()
                {
                    ///  this will verify that the TPE is being added and is added with the correct value  
                    DateTime lastDayLastMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1);
                    string transactionPeriod = lastDayLastMonth.ToString("MMMM - yyyy");

                    _staticFileApiClient
                        .Setup(_ => _.GetContentStream(It.IsAny<string>()))
                        .ReturnsAsync((string a) => new MemoryStream(a.Select(Convert.ToByte).ToArray()) as Stream);

                    _fopApiClient
                        .Setup(_ => _.PostFile(It.IsAny<string>()))
                        .ReturnsAsync((string a) => new MemoryStream(a.Select(Convert.ToByte).ToArray()) as Stream);

                    var xml = XDocument.Parse("<notificationRequest><control><faxRouting /></control></notificationRequest>");

                    _ = await SUT.CreatePdfStream(xml).ConfigureAwait(false);
                    XElement TransactionPeriodElement = xml.XPathSelectElement("/notificationRequest/transactionPeriod");

                    TransactionPeriodElement.Value.Should().Be(transactionPeriod);
                }
            }
        }
    }
}
