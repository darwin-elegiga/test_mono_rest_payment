using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Pdf.Tools;

using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class CustomSendBackDocumentGeneratorServiceTests
    {
        private readonly ICustomSendBackDocumentGeneratorService SUT;

        private readonly DocGenParamInternal testParams;

        private static string TransactionId = "1234";

        public CustomSendBackDocumentGeneratorServiceTests()
        {
            testParams = new DocGenParamInternal
            {
                SavePath = @"/tmp/test",
                DocGenParam = new DocGenParam
                {
                    CheckInfo = new CheckInfo
                    {
                        SETransactionId = TransactionId
                    }
                }
            };

            var fileSystem = new Mock<IFileSystem>();

            var file = new Mock<IFile>();

            file.Setup(_ => _.Exists(It.IsAny<string>())).Returns(true);

            var path = new Mock<IPath>();

            path.Setup(_ => _.Combine(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string a, string b) => $"{a}/{b}");

            fileSystem.Setup(_ => _.File).Returns(file.Object);
            fileSystem.Setup(_ => _.Path).Returns(path.Object);

            var pythonRunner = new Mock<IPythonRunner>();
            var pdfTools = new Mock<IPdfTools>();
            var sendBackCheckXmlFileWriter = new Mock<ISendBackCheckXmlFileWriter>();
            var logger = TestLogger.Create<CustomSendBackDocumentGeneratorService>();

            SUT = new CustomSendBackDocumentGeneratorService(
                pythonRunner.Object,
                fileSystem.Object,
                pdfTools.Object,
                sendBackCheckXmlFileWriter.Object,
                logger);
        }

        [Fact]
        public async Task ShouldHaveSameTransactionId()
        {
            var actual = await SUT.GenerateDocuments(testParams);

            actual
                .Should()
                .OnlyContain(f => f.TransactionId == testParams.DocGenParam.CheckInfo.SETransactionId);
        }

        [Fact]
        public async Task ShouldGenerateCorrectNumberOfCustomSendBackFiles()
        {
            var actual = await SUT.GenerateDocuments(testParams);

            actual
                .Should()
                .HaveCount(2);
        }

        [Theory]
        [MemberData(nameof(CustomSendBackFileMetadata))]
        public async Task ShouldGenerateCorrectCustomSendBackFilesMetadata(CustomSendBackGeneratedFile file)
        {
            var actual = await SUT.GenerateDocuments(testParams);

            actual
                .Should()
                .Contain(f =>
                    f.FileName == file.FileName &&
                    f.Description == file.Description &&
                    f.FileDetailType == file.FileDetailType &&
                    f.VPaygeDocumentType == file.VPaygeDocumentType);
        }

        public static IEnumerable<object[]> CustomSendBackFileMetadata
        {
            get
            {
                return new List<object[]>
                {
                    new []
                    {
                        new CustomSendBackGeneratedFile {
                            VPaygeDocumentType = VPaygeDocumentType.CustomSendBackCheck_Masked,
                            FileName = $@"/tmp/test/CHECK-{TransactionId}.masked.pdf",
                            FileDetailType = FileDetailType.Limited,
                            Description = "Masked check",
                        }
                    },
                    new []
                    {
                        new CustomSendBackGeneratedFile {
                            VPaygeDocumentType = VPaygeDocumentType.CustomSendBackCheck_Clear,
                            FileName = $@"/tmp/test/CHECK-{TransactionId}.clear.pdf",
                            FileDetailType = FileDetailType.Hidden,
                            Description = "Clear check",
                        }
                    },
                };
            }
        }
    }
}
