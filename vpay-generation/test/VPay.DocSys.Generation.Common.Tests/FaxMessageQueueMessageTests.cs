using System;
using System.Text;
using Newtonsoft.Json;
using Xunit;

namespace VPay.DocSys.Generation.Common.Tests
{
    public class FaxMessageQueueMessageTests
    {
        private readonly string _faxMessage =
            "PMB00.00.0001FAXISERIES          FAXNET              FAXDIST             0000000001FAXI000000050001    C548E17B2874102E03A83752F89FB902CALL                2019-07-29-16.06.59.5510000                                                                                             {\"xml\":\"PHRlc3Q+DQo8L3Rlc3Q+\",\"pay\":\"CLVCP\",\"cid\":123,\"tids\":[456],\"faxN\":\"1234567890\",\"ori\":\"RC\",\"ErrorRetryCount\":0}";

        [Fact]
        public void NewFaxMessageShouldMatchOriginalString()
        {
            var result = new FaxMessageQueueMessage(_faxMessage);

            Assert.Equal(_faxMessage, result.ToString());
        }

        [Fact]
        public void ShouldGetCorrectWarrantyXml()
        {
            var result = new FaxMessageQueueMessage(_faxMessage);

            Assert.Equal(
                "<test>\r\n</test>",
                Encoding.UTF8.GetString(Convert.FromBase64String(result.Payload.VPaygeXmlBase64)));
        }


        [Fact]
        public void NewFaxMessageFromPayloadShouldMatchString()
        {
            var actual = new FaxMessageQueueMessage(_faxMessage);
            var payload = JsonConvert.DeserializeObject<FaxMessagePayloadRequest>(_faxMessage.Substring(275));
            payload.Priority = (byte)new Random().Next();
            var expected = new FaxMessageQueueMessage(payload);

            // check that same values are created for both
            Assert.Equal(expected.MessageType, actual.MessageType);
            Assert.Equal(expected.Version, actual.Version);
            Assert.Equal(expected.FromSystem, actual.FromSystem);
            Assert.Equal(expected.ToSystem, actual.ToSystem);
            Assert.Equal(expected.Family, actual.Family);
            Assert.Equal(expected.SmallFamily, actual.SmallFamily);
            Assert.Equal(expected.TypeOfMessage, actual.TypeOfMessage);
            Assert.Equal(expected.Severity, actual.Severity);
            Assert.Equal(expected.MessageIndex, actual.MessageIndex);
            Assert.Equal(expected.FormatVersion, actual.FormatVersion);
            Assert.Equal(expected.OriginAction, actual.OriginAction);
            Assert.Equal(expected.HopCount, actual.HopCount);
            Assert.Equal(expected.OrgFormatId, actual.OrgFormatId);
            Assert.Equal(expected.OrgMessageId, actual.OrgMessageId);
            Assert.Equal(expected.Padding, actual.Padding);
            Assert.Equal(expected.Payload.PaymentType, actual.Payload.PaymentType);
            Assert.Equal(expected.Payload.VPaygeXmlBase64, actual.Payload.VPaygeXmlBase64);

            // check for new value
            Assert.Equal(expected.Priority, payload.Priority);
        }
    }
}
