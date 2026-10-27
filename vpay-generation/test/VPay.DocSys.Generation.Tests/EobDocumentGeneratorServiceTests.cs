using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VPay.DocSys.Common;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Tests.TestModels;
using Xunit;
using System.IO.Abstractions;

namespace VPay.DocSys.Generation.Tests
{
    public class EobDocumentGeneratorServiceTests
    {
        private readonly IEobDocumentGeneratorService SUT;
        private readonly string _resultPath = @"C:\test";

        private readonly MoqFileSystem _fileSystem;
        private readonly Mock<IPdfTools> _pdfTools;
        private readonly Mock<IPythonRunner> _pythonRunner;

        public EobDocumentGeneratorServiceTests()
        {
            _fileSystem = new MoqFileSystem(new Dictionary<string, MockFileData>());

            _pythonRunner = new Mock<IPythonRunner>();
            _pdfTools = new Mock<IPdfTools>();

            SUT = new EobDocumentGeneratorService(
                _fileSystem,
                _pdfTools.Object,
                _pythonRunner.Object,
                TestLogger.Create<EobDocumentGeneratorService>());
        }

        [Fact]
        public async Task GenerateEobDocInternal_WithNoneType_ShouldReturnOnlyTheXmlDocumentAndSaveXmlToFileSystem()
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly().ToDocGenParamInternal(_resultPath, _fileSystem, false);

            var expected = new List<EobGeneratedFile>()
            {
                new EobGeneratedFile
                {
                    FileName = value.EobXmlFilePath,
                    Description = "eob.xml",
                    FileDetailType = FileDetailType.Hidden,
                    TransactionId = value.DocGenParam.StatusRecord.TXID
                }
            };

            var expectedFileName = value.EobXmlFilePath;

            var actual = await SUT.GenerateEobDocuments(EobGenerateType.None, value);

            actual.Should().BeEquivalentTo(expected);

            _fileSystem.FileExists(expectedFileName);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task GenerateEobDocInternal_OnlyPDFType_SavesPDFFileAndReturnsElement(bool hasOverLayer, bool isCheck)
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly().ToDocGenParamInternal(_resultPath, _fileSystem, isCheck);

            if (hasOverLayer)
            {
                value.Config.EobOverlayer = "eobOverlayer";
            }

            var pdfFileName = value.EobXmlFilePath.Replace(".eob.xml", ".plain_eob.pdf");

            _pythonRunner.Setup(c => c.OverlayEob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<DateTime>())).ReturnsAsync(pdfFileName);

            var actual = await SUT.GenerateEobDocuments(EobGenerateType.PdfOnly, value);

            var expected = new List<EobGeneratedFile>()
            {
                new EobGeneratedFile
                {
                    FileName = value.EobXmlFilePath,
                    Description = "eob.xml",
                    FileDetailType = FileDetailType.Hidden,
                    TransactionId = value.DocGenParam.StatusRecord.TXID
                },
                new EobGeneratedFile()
                {
                    FileName = pdfFileName,
                    EobGenerateType = EobGenerateType.PdfOnly,
                    FileDetailType = FileDetailType.Pdf,
                    Description = "Original PDF",
                    TransactionId = value.DocGenParam.StatusRecord.TXID
                }
            };

            actual.Should().BeEquivalentTo(expected);

            foreach (var file in expected)
            {
                _fileSystem.File.Exists(file.FileName);
            }
        }

        [Fact]
        public async Task GenerateEobDocInternal_OnlyXMLOnly_RunsEOBPythonProcessorAndReturnsElement()
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly().ToDocGenParamInternal(_resultPath, _fileSystem, false);

            var expectedFileName = value.EobXmlFilePath.Replace(".eob.xml", ".plain_eob.pdf");

            _pythonRunner.Setup(x => x.CreateEob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>()))
                .ReturnsAsync(expectedFileName);

            var actual = await SUT.GenerateEobDocuments(EobGenerateType.DataOnly, value);

            var expected = new List<EobGeneratedFile>()
            {
                new EobGeneratedFile
                {
                    FileName = value.EobXmlFilePath,
                    Description = "eob.xml",
                    FileDetailType = FileDetailType.Hidden,
                    TransactionId = value.DocGenParam.StatusRecord.TXID
                },
                new EobGeneratedFile()
                {
                    FileName = expectedFileName,
                    EobGenerateType = EobGenerateType.DataOnly,
                    FileDetailType = FileDetailType.Pdf,
                    Description = "plain_eob.pdf",
                    TransactionId = value.DocGenParam.StatusRecord.TXID
                }
            };

            actual.Should().BeEquivalentTo(expected);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GenerateEobDocInternal_WithPdfAndData(bool isDataThenPdf)
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly().ToDocGenParamInternal(_resultPath, _fileSystem, false);

            var expectedFileName = value.EobXmlFilePath.Replace(".eob.xml", Storage.Constants.FileExtensions.EobPdf);
            var expectedDataFileName = value.EobXmlFilePath.Replace(".eob.xml", Storage.Constants.FileExtensions.EobTempDataPdf);
            var expectedPdfFileName = value.EobXmlFilePath.Replace(".eob.xml", Storage.Constants.FileExtensions.EobTempBase64Pdf);

            _pythonRunner.Setup(x => x.CreateEob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>()))
                .ReturnsAsync(expectedDataFileName);

            _pdfTools.Setup(x => x.MergeAsync(expectedFileName, It.IsAny<IList<string>>()))
                .ReturnsAsync(true);

            var eobType = isDataThenPdf ? EobGenerateType.DataThenPdf : EobGenerateType.PdfThenData;

            var actual = await SUT.GenerateEobDocuments(eobType, value);

            var expected = new List<EobGeneratedFile>()
            {
                new EobGeneratedFile
                {
                    FileName = value.EobXmlFilePath,
                    Description = "eob.xml",
                    FileDetailType = FileDetailType.Hidden,
                    TransactionId = value.DocGenParam.StatusRecord.TXID
                },
                new EobGeneratedFile()
                {
                    FileName = expectedFileName,
                    EobGenerateType = eobType,
                    FileDetailType = FileDetailType.Pdf,
                    Description = "Original plus composed PDF",
                    TransactionId = value.DocGenParam.StatusRecord.TXID
                }
            };

            _pythonRunner.VerifyAll();
            _pdfTools.VerifyAll();

            _fileSystem.FileMoq.Verify(x => x.Delete(expectedDataFileName), Times.Once);
            _fileSystem.FileMoq.Verify(x => x.Delete(expectedPdfFileName), Times.Once);

            actual.Should().BeEquivalentTo(expected);

            if (isDataThenPdf)
            {
                _pdfTools.Verify(x => x.MergeAsync(expectedFileName, new[] { expectedDataFileName, expectedPdfFileName }), Times.Once);
            }
            else
            {
                _pdfTools.Verify(x => x.MergeAsync(expectedFileName, new[] { expectedPdfFileName, expectedDataFileName }), Times.Once);
            }
        }
    }
}
