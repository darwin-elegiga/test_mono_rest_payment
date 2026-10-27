using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Models;
using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public static class WarrantyPdfCreatorTests
    {
        public static class WhenCreatePdfStream
        {
            public class ShouldThrowError
            {
                private Mock<IStaticFileApiClient> _staticFileApiClient = new Mock<IStaticFileApiClient>(MockBehavior.Strict);
                private Mock<IFopApiClient> _fopApiClient = new Mock<IFopApiClient>(MockBehavior.Strict);
                private Mock<FopBaseXsl> _fopBaseXml = new Mock<FopBaseXsl>(MockBehavior.Strict);

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

                private Mock<ILogger<WarrantyPdfCreator>> _logger = new Mock<ILogger<WarrantyPdfCreator>>(MockBehavior.Loose);

                private IWarrantyPdfCreator SUT => new WarrantyPdfCreator(
                    _staticFileApiClient.Object,
                    _fopApiClient.Object,
                    _fopBaseXml.Object,
                    _apiSettings,
                    _logger.Object);

                [Fact]
                public async Task IfXslFilePathDoesNotExist()
                {
                    var xml = XDocument.Parse("<test></test>");

                    await Assert.ThrowsAsync<Exception>(() => SUT.CreatePdfStream("TEST", xml));
                }

                [Fact]
                public async Task IfControlTypeElementIsNotFax()
                {
                    var xslt = @"<?xml version=""1.0""?>
                        <xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"" xmlns:fo=""http://www.w3.org/1999/XSL/Format"">
                            <xsl:template match=""test"">
                            </xsl:template>
                        </xsl:stylesheet>";

                    var test = "TEST".Select(Convert.ToByte).ToArray();

                    _staticFileApiClient
                        .Setup(_ => _.GetContentStream(It.IsAny<string>()))
                        .ReturnsAsync(new MemoryStream(xslt.Select(Convert.ToByte).ToArray()) as Stream);

                    _fopApiClient
                        .Setup(_ => _.PostFile(It.IsAny<string>()))
                        .ReturnsAsync(new MemoryStream(test) as Stream);

                    var xml = XDocument.Parse("<test></test>");

                    var result = await SUT.CreatePdfStream("TEST", xml);

                    result.ToArray().Should()
                        .Equal(test);
                }
            }
        }
    }
}
