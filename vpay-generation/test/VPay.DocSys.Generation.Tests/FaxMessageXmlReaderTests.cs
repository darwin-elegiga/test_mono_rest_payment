using System;
using System.Xml.Linq;
using System.Xml.XPath;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.DocSys.Generation.Common;
using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class FaxMessageXmlReaderTests
    {
        static Mock<ILogger<FaxMessageXmlReader>> _logger = new Mock<ILogger<FaxMessageXmlReader>>(MockBehavior.Loose);

        static IFaxMessageXmlReader SUT => new FaxMessageXmlReader(
           _logger.Object);

        public class WhenReadingXml
        {
            static string xml =>
                @"<notificationRequest>
                    <control>
                        <type>FAX</type>
                        <clientCode>TPA</clientCode>
                        <Billing_Entity>BE</Billing_Entity>
                        <notificationID>789789</notificationID>
                        <application>APP</application>
                    </control>
                    <card>
                        <loadTransactionID>123123</loadTransactionID>
                    </card>
                    <sendTo>
                        <fax>1234567890</fax>
                    </sendTo>
                </notificationRequest>";

            [Fact]
            public void ShouldBeSuccessful()
            {
                var result = SUT.ReadXml(xml);

                result.Xml.ToString().Should().Be(XDocument.Parse(xml).ToString());
                result.Tpa.Should().Be("TPA");
                result.Type.Should().Be("FAX");
                result.BillingEntity.Should().Be("BE");
                result.Application.Should().Be("APP");
                result.TransactionId.Should().Be("123123");
                result.CorrespondenceId.Should().Be("789789");
                result.FaxNumber.Should().Be("1234567890");
                result.XslUrlPath.Should().Be($"TPA/xsl/TPA.app.fax.xsl");
            }

            public class ShouldThrowError
            {
                [Fact]
                public void IfNoControlNodeInXml()
                {
                    var xd = XDocument.Parse(xml);
                    xd.XPathSelectElement(".//control").Remove();

                    Assert.Throws<ArgumentNullException>(() => SUT.ReadXml(xd.ToString()));
                }

                [Fact]
                public void IfNoCardNodeInXml()
                {
                    var xd = XDocument.Parse(xml);
                    xd.XPathSelectElement(".//card").Remove();

                    Assert.Throws<ArgumentNullException>(() => SUT.ReadXml(xd.ToString()));
                }

                [Fact]
                public void IfNoSendToNodeInXml()
                {
                    var xd = XDocument.Parse(xml);
                    xd.XPathSelectElement(".//sendTo").Remove();

                    Assert.Throws<ArgumentNullException>(() => SUT.ReadXml(xd.ToString()));
                }


                [Fact]
                public void IfTypeIsNotFAX()
                {
                    var xd = xml.Replace("<type>FAX</type>", "<type>TYPE</type>");

                    Assert.Throws<Exception>(() => SUT.ReadXml(xd));
                }
            }
        }
    }
}
