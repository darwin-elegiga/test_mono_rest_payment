using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using VPay.Distribution.Common.Helpers;
using VPay.DocSys.Common;
using VPay.DocSys.Storage;
using Xunit;

namespace VPay.Distribution.Tests
{
    public class StatusRecordTests
    {
        #region GetPacketArchive

        [Fact]
        public void GetPacketArchive_WithNullDetails_ShouldReturnsNull()
        {
            var statusRecord = TestHelpers.GetStatusRecords(1).First();
            statusRecord.ArchiveFile = null;

            var actual = statusRecord.GetPacketArchive();

            actual.Should().BeNull();
        }

        [Fact]
        public void GetPacketArchive_WithNullPayType_ShouldReturnsNull()
        {
            var statusRecord = TestHelpers.GetStatusRecords(1).First();
            statusRecord.PAYTYPE = null;

            var actual = statusRecord.GetPacketArchive();

            actual.Should().BeNull();
        }

        [Theory]
        [InlineData(PayType.CLCCK)]
        [InlineData(PayType.CLCHK)]
        [InlineData(PayType.CLNPF)]
        [InlineData(PayType.CLEFT)]
        [InlineData(PayType.CLSDA)]
        [InlineData(PayType.CLCFT)]
        [InlineData(PayType.CLVCP)]
        [InlineData(PayType.OPACH)]
        public void GetPacketArchive_WithExactlyOneRecordForPayType_ShouldReturnRecordWithPayType(string payType)
        {
            var statusRecord = TestHelpers.GetStatusRecords(1).First();
            statusRecord.PAYTYPE = payType;

            statusRecord.ArchiveFile.ArchiveDetails = new List<ArchiveDetail>()
            {
                new ArchiveDetail
                {
                    ArchiveDetailId = 2,
                    ArchiveFileId = 1,
                    FileDetailType = FileDetailType.Pdf,
                    Description = "plain_eob",
                    FileName = "F34343_Test.eob.pdf"
                },
                new ArchiveDetail
                {
                    ArchiveDetailId = 1,
                    ArchiveFileId = 1,
                    FileDetailType = FileDetailType.Pdf,
                    Description = "Check + EOB",
                    FileName = "F34343_Test" + PayTypeExtensions.ToFileExtension(payType)
                }
            };

            var expected = statusRecord.ArchiveFile.ArchiveDetails.Cast<ArchiveDetail>().ToList()[1];

            var actual = statusRecord.GetPacketArchive();

            actual.Should().Be(expected);
        }

        [Theory]
        [InlineData(PayType.CLCCK)]
        [InlineData(PayType.CLCHK)]
        [InlineData(PayType.CLNPF)]
        [InlineData(PayType.CLEFT)]
        [InlineData(PayType.CLSDA)]
        [InlineData(PayType.CLCFT)]
        [InlineData(PayType.CLVCP)]
        [InlineData(PayType.OPACH)]
        public void GetPacketArchive_WithMulipleRecordsForPayType_ShouldReturnRecordWithPayTypeThatHasHigherArchiveDetailId(string payType)
        {
            var statusRecord = TestHelpers.GetStatusRecords(1).First();
            statusRecord.PAYTYPE = payType;

            statusRecord.ArchiveFile.ArchiveDetails = new List<ArchiveDetail>()
            {
                new ArchiveDetail
                {
                    ArchiveDetailId = 3,
                    ArchiveFileId = 1,
                    FileDetailType = FileDetailType.Pdf,
                    Description = "Check + EOB",
                    FileName = "134343_Test" + PayTypeExtensions.ToFileExtension(payType)
                },
                new ArchiveDetail
                {
                    ArchiveDetailId = 1,
                    ArchiveFileId = 1,
                    FileDetailType = FileDetailType.Pdf,
                    Description = "Check + EOB",
                    FileName = "F34343_Test" + PayTypeExtensions.ToFileExtension(payType)
                },
                new ArchiveDetail
                {
                    ArchiveDetailId = 2,
                    ArchiveFileId = 1,
                    FileDetailType = FileDetailType.Pdf,
                    Description = "plain_eob",
                    FileName = "F34343_Test.eob.pdf"
                },
            };

            var expected = statusRecord.ArchiveFile.ArchiveDetails.First();

            var actual = statusRecord.GetPacketArchive();

            actual.Should().Be(expected);
        }

        #endregion GetPacketArchive
    }
}
