using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using Xunit;
using Moq;
using VPay.DocSys.Parsing.Common;

namespace VPay.DocSys.Parsing.Tests
{
    public class inbound_835_file_parser_should
    {
        private readonly IFileSystem _fileSystem;

        private readonly Mock<IFile> _mockFile;
        private readonly Mock<IFileSystem> _mockFileSystem;

        private readonly IInbound835FileParser SUT;

        public inbound_835_file_parser_should()
        {
            _fileSystem = new FileSystem();

            _mockFile = new Mock<IFile>();
            _mockFile.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);

            _mockFileSystem = new Mock<IFileSystem>();
            _mockFileSystem.SetupGet<IFile>(fs => fs.File).Returns(_mockFile.Object);
            _mockFileSystem.SetupGet<IPath>(fs => fs.Path).Returns(_fileSystem.Path);

            SUT = new Inbound835FileParser(_mockFileSystem.Object);
        }


        [Theory]
        [InlineData("/my/test/path/202010241532.txt", Inbound835FileType.PaymentPosted)]
        [InlineData("/my/test/path/202010252455.txt", Inbound835FileType.PaymentPosted)]
        [InlineData("/my/test/path/202010262349.835.txt", Inbound835FileType.PaymentPosted)]
        [InlineData("/my/test/path/202010271722.835.txt", Inbound835FileType.PaymentPosted)]

        [InlineData("/my/test/path/ERA_AEG_1013080894_00000000000896166662_20201027190354.835", Inbound835FileType.RemittanceAdvice)]
        [InlineData("/my/test/path/ERA_TLC_844449843_00000000000895512000_20201027164939.835", Inbound835FileType.RemittanceAdvice)]

        [InlineData("/my/test/path/561479712_RC_TLC_TLC0001866_1094001LO2020102699019250A202001333306411_895505346_NEW_20201026143452.txt", Inbound835FileType.Acknowledgment)]
        [InlineData("/my/test/path/202265068_RC_HBI_HBI0001001_1290001RC2020101453000717A202001323866761_894322699_NEW_20201023121211.txt", Inbound835FileType.Acknowledgment)]

        [InlineData("/my/test/path/954434428_SEMED_HEQTN_HEQTN01001_FKPGY5W_895899215_NEW_20201027042249.ack", Inbound835FileType.Acknowledgment)]
        [InlineData("/my/test/path/990107330_IC_SWK_SWKPF01001_A0007182412MEMAX07_896198860_NEW_20201027123147.ack", Inbound835FileType.Acknowledgment)]
        [InlineData("/my/test/path/990373279_RC_AEG_AEG0001007_1305001AE2020102300024858A202001332990911_895476039_NEW_20201026151310.ack", Inbound835FileType.Acknowledgment)]
        [InlineData("/my/test/path/990373279_RC_AEG_AEG0001007_1305001AE2020102300024858A202001332990911_895476039_NEW_20201026151310XXX.ack", Inbound835FileType.Acknowledgment)]

        [InlineData("/my/test/path/561479712_RC_TLC_TLC0001866_1094001LO2020102699019250A202001333306411_895505346_RI_20201026143452.txt", Inbound835FileType.PaymentPosted)]
        [InlineData("/my/test/path/990373279_RC_AEG_AEG0001007_1305001AE2020102300024858A202001332990911_895476039_RI_20201026151310.txt", Inbound835FileType.PaymentPosted)]
        [InlineData("/my/test/path/561479712_RC_TLC_TLC0001866_1094001LO2020102699019250A202001333306411_895505346_NEW_20201026143452X.txt", Inbound835FileType.PaymentPosted)]

        [InlineData("/my/test/path/561479712_RC_TLC_TLC0001866_1094001LO2020102699019250A202001333306411_895505346_NEW_20201026143452.xxx", Inbound835FileType.Unknown)]
        [InlineData("/my/test/path/561479712_RC_TLC_TLC0001866_1094001LO2020102699019250A202001333306411_895505346_NEW_20201026143452", Inbound835FileType.Unknown)]
        public void map_file_path_to_appropriate_file_type(string filePath, Inbound835FileType expectedFileType)
        {
            var inbound835FileType = SUT.GetFileType(filePath);

            inbound835FileType.Should().Be(expectedFileType, $"this is the type that should be associated with '{Path.GetFileName(filePath)}'");
        }
    }
}
