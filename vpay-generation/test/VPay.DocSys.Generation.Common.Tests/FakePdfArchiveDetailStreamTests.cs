using System;
using System.IO;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common.Edi835;
using Xunit;

namespace VPay.DocSys.Generation.Common.Tests
{
    public class FakePdfArchiveDetailStreamTests
    {

        [Fact]
        public void ShouldGenerateFakePdf()
        {
            // This should be the bare minimum to create a valid BaseFilename...
            var edi835CreationMessage = new Edi835CreationMessage
            {
                TaxIdentificationNumber = "123456789",
                OriginCode = "IC",
                ClientCode = "SWK",
                BillingEntityCode = "SWK00001",
                UserKey = "ABC123",
                TransactionId = 1000001,
                CheckStatus = "NEW",
                TimeStamp = new DateTime(2020, 1, 1, 1, 1, 1)
            };
            var expectedFilename = edi835CreationMessage.GetBaseFilename() + ".pdf";
            var expectedContentType = "text/plain";
            var expectedContent = "Fake PDF";
            FakePdfArchiveDetailStream SUT = new FakePdfArchiveDetailStream(edi835CreationMessage);

            Assert.Equal(expectedFilename, SUT.FileName);
            Assert.Equal(expectedContentType, SUT.ContentType);

            using (var streamReader = new StreamReader(SUT.Open()))
            {
                var actual = streamReader.ReadToEnd();
                Assert.Equal(expectedContent, actual);
                streamReader.Close();
            }
        }
    }
}
