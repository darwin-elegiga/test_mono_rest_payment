using System;
using System.Collections.Generic;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class CoRecordTest
    {
        private static CoRecord Sut;

        public class WhenGettingRevisionMetadata
        {
            public WhenGettingRevisionMetadata()
            {
                Sut = new CoRecord(new InventoryDriverRecordMessage
                {
                    Guid = Guid.Parse("0f00715d-c010-47a6-ac63-fb4b753d1383"),
                    DocumentType = RedCardDocumentType.CO,
                    PdfFiles = new KeyValuePair<string, string>[0],
                    DocumentFileNames = new List<string> {"TestFileName.pdf"},
                    InventoryDriverFilePath = null,
                    TransactionId = 526714798,
                    Tpa = "TVT",
                    PaymentId = "1"
                });
            }

            [Fact]
            public void ShouldThrowExceptionWhenNullFileList()
            {
                Assert.Throws<ArgumentNullException>(() => Sut.GetRevisionMetadata(null));
            }

            [Fact]
            public void ShouldThrowExceptionWhenEmptyFileList()
            {
                Assert.Throws<ArgumentException>(() => Sut.GetRevisionMetadata(new List<string>()));
            }

            [Fact]
            public void ShouldReturnStandardRevisionMetadataWhenNoMatchingFileNames()
            {
                var actual = Sut.GetRevisionMetadata(new List<string> { "" });

                Assert.Equal(1, actual.Revision);
                Assert.Equal("1_CO_stapled.pdf", actual.FileName);
                Assert.Equal("stapled mailed pdf documents(s)", actual.Description);
            }

            [Fact]
            public void ShouldReturnCorrectRevisionMetadataWhenMatchingFileNameOnce()
            {
                var actual = Sut.GetRevisionMetadata(new[] { "1_CO_stapled.pdf" });

                Assert.Equal(2, actual.Revision);
                Assert.Equal("1_CO_stapled_02.pdf", actual.FileName);
                Assert.Equal("stapled mailed pdf documents(s) v02", actual.Description);
            }

            [Fact]
            public void ShouldReturnCorrectRevisionMetadataWhenMultipleMatchingFileNames()
            {
                var actual = Sut.GetRevisionMetadata(new[]
                {
                    "1_CO_stapled.pdf",
                    "1_CO_stapled_02.pdf",
                    "1_CO_stapled_03.pdf",
                    "1_CO_stapled_04.pdf"
                });

                Assert.Equal(5, actual.Revision);
                Assert.Equal("1_CO_stapled_05.pdf", actual.FileName);
                Assert.Equal("stapled mailed pdf documents(s) v05", actual.Description);
            }
        }
    }
}
