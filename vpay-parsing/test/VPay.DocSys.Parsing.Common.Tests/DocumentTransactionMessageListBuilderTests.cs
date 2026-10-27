using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common.Interfaces;
using Xunit;

namespace VPay.DocSys.Parsing.Common.Tests
{
    public class DocumentTransactionMessageListBuilderTests
    {
        protected readonly IDocumentTransactionMessageListBuilder SUT;

        protected readonly Mock<ISubFileParserService> _subFileParserService = new Mock<ISubFileParserService>();
        protected readonly Mock<ICheckFileParser> _checkFileParser = new Mock<ICheckFileParser>();
        protected readonly Mock<IOtbService> _otbService = new Mock<IOtbService>();

        protected readonly ILogger<DocumentTransactionMessageListBuilder> _logger = TestLogger.Create<DocumentTransactionMessageListBuilder>();

        protected const string _transactionId = "123456789";

        protected DocumentTransactionMessageListBuilderTests()
        {
            SUT = new DocumentTransactionMessageListBuilder(
                _subFileParserService.Object,
                _checkFileParser.Object,
                _otbService.Object,
                _logger);

            _checkFileParser.Setup(x => x.ParseCheckFileAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(new List<CheckInfo>
                {
                    new CheckInfo
                    {
                        SETransactionId = _transactionId
                    },
                });

            _subFileParserService.Setup(x => x.ParseFile(It.IsAny<string>()))
                .Returns(new Dictionary<string, SubstituteDataInfo>
                {
                    { _transactionId, new SubstituteDataInfo() },
                });

            _otbService
                .Setup(x => x.GetOtbRecordsAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync((string _, string[] a) => new OtbRecords(a
                    .Distinct()
                    .ToDictionary(
                        k => k,
                        v => (IDictionary<string, string>)new Dictionary<string, string>(0))));
        }

        protected async Task<List<DocumentTransactionMessage>> BuildMessages(bool isRedCard, bool isCheckPayType, bool isCustomSendBackTpa, bool isSendBackDistType, bool isSmbUnsafeTpa)
        {
            var tpa = isSmbUnsafeTpa ? "CON" : "TEST";

            var payType = isCheckPayType
                ? PayType.CLCHK
                : PayType.CLEFT;

            var distType = isSendBackDistType
                ? DistType.SendBack
                : DistType.Mail;

            var dc = new DocSysConfiguration();
            var sf = new StatusFile
            {
                Header = new StatusHeader(),
                Footer = new StatusFooter(),
                // we add the same record twice to check that only one distinct record is actually used for the check info xml parsing
                Records = new List<StatusRecord>
                {
                    new StatusRecord
                    {
                        PAYTYPE = payType,
                        TXID = _transactionId,
                        DESC = "TEST",
                        BILENT = "TEST",
                        TPA = tpa,
                        ORIGIN = "TEST",
                        DIST = distType
                    },
                    new StatusRecord
                    {
                        PAYTYPE = payType,
                        TXID = _transactionId,
                        DESC = "TEST",
                        BILENT = "TEST",
                        TPA = tpa,
                        ORIGIN = "TEST",
                        DIST = distType
                    },
                }
            };

            var ids = sf.Records.Select(_ => Convert.ToInt64(_.TXID)).ToList();

            return await SUT.BuildMessagesAsync(dc, sf, @"c:\test", ids, false, false, isCustomSendBackTpa, isRedCard).ConfigureAwait(false);
        }

        public class SetCheckInfo : DocumentTransactionMessageListBuilderTests
        {
            public SetCheckInfo() : base() { }

            [Theory]
            [InlineData(false, false, false, false, false)]
            [InlineData(false, false, false, false, true)]
            [InlineData(false, false, false, true, false)]
            [InlineData(false, false, false, true, true)]

            [InlineData(false, false, true, false, false)]
            [InlineData(false, false, true, false, true)]

            [InlineData(true, false, false, false, false)]
            [InlineData(true, false, false, false, true)]
            [InlineData(true, false, false, true, false)]
            [InlineData(true, false, false, true, true)]

            [InlineData(true, false, true, false, false)]
            [InlineData(true, false, true, false, true)]

            [InlineData(true, true, false, false, false)]
            [InlineData(true, true, false, false, true)]
            [InlineData(true, true, false, true, false)]
            [InlineData(true, true, false, true, true)]

            [InlineData(true, true, true, false, false)]
            [InlineData(true, true, true, false, true)]
            public async Task ShouldNotParseCheckXml(bool isRedCard, bool isCheckPayType, bool isCustomSendBackTpa, bool isSendBackDistType, bool isSmbUnsafeTpa)
            {
                var result = await BuildMessages(isRedCard, isCheckPayType, isCustomSendBackTpa, isSendBackDistType, isSmbUnsafeTpa).ConfigureAwait(false);

                _checkFileParser.Verify(x => x.ParseCheckFileAsync(It.IsAny<string>(), isCustomSendBackTpa, isRedCard), Times.Never);

                Assert.Equal(2, result.Count);
                Assert.Equal(isRedCard, result[0].IsRedCard);
                Assert.Equal(isCustomSendBackTpa && isSendBackDistType, result[0].IsCustomSendBack);
                Assert.Null(result[0].CheckInfo);
            }

            [Theory]
            [InlineData(false, false, true, true, false)]
            [InlineData(false, false, true, true, true)]

            [InlineData(false, true, false, false, false)]
            [InlineData(false, true, false, false, true)]
            [InlineData(false, true, false, true, false)]
            [InlineData(false, true, false, true, true)]

            [InlineData(false, true, true, false, false)]
            [InlineData(false, true, true, false, true)]
            [InlineData(false, true, true, true, false)]
            [InlineData(false, true, true, true, true)]

            [InlineData(true, false, true, true, false)]
            [InlineData(true, false, true, true, true)]

            [InlineData(true, true, true, true, false)]
            [InlineData(true, true, true, true, true)]
            public async Task ShouldParseCheckXml(bool isRedCard, bool isCheckPayType, bool isCustomSendBackTpa, bool isSendBackDistType, bool isSmbUnsafeTpa)
            {
                var result = await BuildMessages(isRedCard, isCheckPayType, isCustomSendBackTpa, isSendBackDistType, isSmbUnsafeTpa).ConfigureAwait(false);

                _checkFileParser.Verify(x => x.ParseCheckFileAsync(It.IsAny<string>(), isCustomSendBackTpa, isRedCard), Times.Once);

                Assert.Equal(2, result.Count);
                Assert.Equal(isRedCard, result[0].IsRedCard);
                Assert.Equal(isCustomSendBackTpa && isSendBackDistType, result[0].IsCustomSendBack);
                Assert.NotNull(result[0].CheckInfo);
            }
        }

        public class GetOtbRecords : DocumentTransactionMessageListBuilderTests
        {
            public GetOtbRecords() : base() { }

            [Theory]
            [MemberData(nameof(AllMessages))]
            public async Task ShouldAlwaysFindAnOtbRecord(bool isRedCard, bool isCheckPayType, bool isCustomSendBackTpa, bool isSendBackDistType, bool isSmbUnsafeTpa)
            {
                var messages = await BuildMessages(isRedCard, isCheckPayType, isCustomSendBackTpa, isSendBackDistType, isSmbUnsafeTpa).ConfigureAwait(false);

                messages.Should().NotBeNullOrEmpty();
                messages.Select(x => x.OTBData).Should().NotContainNulls();
            }

            [Fact]
            public async Task ShouldThrowExceptionForMissingOtbRecord()
            {
                _otbService
                    .Setup(c => c.GetOtbRecordsAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                    .ReturnsAsync(new OtbRecords());

                await Assert.ThrowsAsync<KeyNotFoundException>(async () => await BuildMessages(true, true, true, true, true).ConfigureAwait(false)).ConfigureAwait(false);
            }

            public static IEnumerable<object[]> AllMessages()
            {
                var bools = new bool[] { true, false };

                return from isRedCard in bools
                       from isCheckPayType in bools
                       from isCustomSendBackTpa in bools
                       from isSendBackDistType in bools
                       from isSmbUnsafeTpa in bools
                       select new object[] { isRedCard, isCheckPayType, isCustomSendBackTpa, isSendBackDistType, isSmbUnsafeTpa };
            }
        }
    }
}
