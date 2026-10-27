using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Interfaces;
using VPay.DocSys.Generation.Configs;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Generation.Tests.TestModels;
using VPay.DocSys.Storage;
using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class DocSysOperationsTests
    {
        private readonly DocSysSettings _docSysSettings;
        private readonly Mock<DocSysOperations> _opsMock;
        private readonly Mock<IEobDocumentGeneratorService> _eobDocumentGeneratorService;

        private DocSysOperations _ops => _opsMock.Object;

        public DocSysOperationsTests()
        {
            _docSysSettings = new DocSysSettings
            {
                WhiteBoxPdf = @"C:\test\whiteBox.pdf"
            };

            _eobDocumentGeneratorService = new Mock<IEobDocumentGeneratorService>();

            var fileSystem = new MoqFileSystem(new Dictionary<string, MockFileData>() { { _docSysSettings.WhiteBoxPdf, new MockFileData(new byte[] { 0x12, 0x34, 0x56, 0xd2 }) } });
            var packetDocumentGeneratorService = new Mock<IPacketDocumentGeneratorService>();
            var eobXmlParser = new Mock<EobXmlParser>() { CallBase = true };
            var storageOperations = new Mock<IStorageManager>();
            var customSendBackDocumentGeneratorService = new Mock<ICustomSendBackDocumentGeneratorService>();
            var coverVPaygeGeneratorService = new Mock<ICoverVPaygeGeneratorService>();
            var logger = TestLogger.Create<DocSysOperations>();
            var redCardRepository = new Mock<IRedCardRepository>();
            var cleanDocMock = new Mock<CleanDocsService>(null, null, null, null);
            var fssiTracManagerMock = new Mock<IFssiTracManager>();
            var stpRemittanceDocumentService = new Mock<IStpRemittanceDocumentService>();

            fssiTracManagerMock.Setup(x => x.SentToFssi(It.IsAny<string>())).ReturnsAsync(false);
            fileSystem.Directory.CreateDirectory("C:\\test");

            _opsMock = new Mock<DocSysOperations>(
                eobXmlParser.Object,
                (IFileSystem)fileSystem,
                storageOperations.Object,
                logger,
                _docSysSettings,
                cleanDocMock.Object,
                redCardRepository.Object,
                customSendBackDocumentGeneratorService.Object,
                coverVPaygeGeneratorService.Object,
                _eobDocumentGeneratorService.Object,
                packetDocumentGeneratorService.Object,
                fssiTracManagerMock.Object,
                stpRemittanceDocumentService.Object)
            { CallBase = true };
        }

        #region GetCustomPdfElement

        [Fact]
        public void GetCustomPdfElement_WithoutCustomPdfElement_ReturnsNull()
        {
            var eobXml = new Dictionary<string, dynamic>();

            var actual = DocSysOperations.GetCustomPdfElement(eobXml);

            actual.Should().BeNull();
        }

        [Fact]
        public void GetCustomPdfElement_CustomPdfWithoutOrderElement_ReturnsElement()
        {
            var eob = new Dictionary<string, dynamic>
            {
                {
                    "custom_pdfs", new Dictionary<string, dynamic>
                    {
                        { "method", "APPEND" },
                        { "duplex_print", "Yes" },
                        {
                            "files", new Dictionary<string, dynamic>
                            {
                                { "file", "/home/bogner/eig/C36_modified.pdf" }
                            }
                        }
                    }
                }
            };

            var actual = DocSysOperations.GetCustomPdfElement(eob);

            var expected = new EobXmlCustomPdfs()
            {
                Method = "APPEND",
                DuplexPrint = "Yes",
                Order = null,
                Files = new CustomPdfFile[]
                {
                    new CustomPdfFile
                    {
                        File = "/home/bogner/eig/C36_modified.pdf"
                    }
                }
            };

            actual.Should().BeEquivalentTo(expected, options => options.Excluding(o => o.OrderAsString));
        }

        [Fact]
        public void GetCustomPdfElement_CustomPdfWithoutDuplexPrintElement_ReturnsElement()
        {
            var eob = new Dictionary<string, dynamic>
            {
                {
                    "custom_pdfs", new Dictionary<string, dynamic>
                    {
                        { "method", "APPEND" },
                        { "order", "1" },
                        {
                            "files", new Dictionary<string, dynamic>
                            {
                                { "file", "/home/bogner/eig/C36_modified.pdf" }
                            }
                        }
                    }
                }
            };

            var actual = DocSysOperations.GetCustomPdfElement(eob);

            var expected = new EobXmlCustomPdfs()
            {
                Method = "APPEND",
                Order = 1,
                Files = new CustomPdfFile[]
                {
                    new CustomPdfFile
                    {
                        File = "/home/bogner/eig/C36_modified.pdf"
                    }
                }
            };

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void GetCustomPdfElement_CustomPdfWithBlankOrderElement_ReturnsElement()
        {
            var eob = new Dictionary<string, dynamic>
            {
                {
                    "custom_pdfs", new Dictionary<string, dynamic>
                    {
                        { "method", "APPEND" },
                        { "duplex_print", "Yes" },
                        { "order", null },
                        {
                            "files", new Dictionary<string, dynamic>
                            {
                                { "file", "/home/bogner/eig/C36_modified.pdf" }
                            }
                        }
                    }
                }
            };
            var actual = DocSysOperations.GetCustomPdfElement(eob);

            var expected = new EobXmlCustomPdfs()
            {
                Method = "APPEND",
                DuplexPrint = "Yes",
                Order = null,
                Files = new CustomPdfFile[]
                {
                    new CustomPdfFile
                    {
                        File = "/home/bogner/eig/C36_modified.pdf"
                    }
                }
            };

            actual.Should().BeEquivalentTo(expected, options => options.Excluding(o => o.OrderAsString));
        }

        [Fact]
        public void GetCustomPdfElement_CustomPdfWithOrderElement_ReturnsElement()
        {
            var eob = new Dictionary<string, dynamic>
            {
                {
                    "custom_pdfs", new Dictionary<string, dynamic>
                    {
                        { "method", "APPEND" },
                        { "duplex_print", "Yes" },
                        { "order", "1" },
                        {
                            "files", new Dictionary<string, dynamic>
                            {
                                { "file", "/home/bogner/eig/C36_modified.pdf" }
                            }
                        }
                    }
                }
            };

            var actual = DocSysOperations.GetCustomPdfElement(eob);

            var expected = new EobXmlCustomPdfs()
            {
                Method = "APPEND",
                DuplexPrint = "Yes",
                Order = 1,
                Files = new CustomPdfFile[]
                {
                    new CustomPdfFile
                    {
                        File = "/home/bogner/eig/C36_modified.pdf"
                    }
                }
            };

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void GetCustomPdfElement_CustomPdfWithoutFilesElement_ReturnsElement()
        {
            var eob = new Dictionary<string, dynamic>
            {
                {
                    "custom_pdfs", new Dictionary<string, dynamic>
                    {
                        { "method", "APPEND" },
                        { "duplex_print", "Yes" },
                        { "order", "1" }
                    }
                }
            };

            var actual = DocSysOperations.GetCustomPdfElement(eob);

            var expected = new EobXmlCustomPdfs()
            {
                Method = "APPEND",
                DuplexPrint = "Yes",
                Order = 1
            };

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void GetCustomPdfElement_CustomPdfWithBlankFilesElement_ReturnsElement()
        {
            var eob = new Dictionary<string, dynamic>
            {
                {
                    "custom_pdfs", new Dictionary<string, dynamic>
                    {
                        { "method", "APPEND" },
                        { "duplex_print", "Yes" },
                        { "order", "1" },
                        { "files",  null }
                    }
                }
            };

            var actual = DocSysOperations.GetCustomPdfElement(eob);

            var expected = new EobXmlCustomPdfs()
            {
                Method = "APPEND",
                DuplexPrint = "Yes",
                Order = 1,
                Files = null
            };

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void GetCustomPdfElement_CustomPdfWithMultipleFileElements_ReturnsElement()
        {
            var eob = new Dictionary<string, dynamic>
            {
                {
                    "custom_pdfs", new Dictionary<string, dynamic>
                    {
                        { "method", "APPEND" },
                        { "duplex_print", "Yes" },
                        { "order", "1" },
                        {
                            "files", new Dictionary<string, dynamic>
                            {
                                {
                                    "file", new List<object>{
                                        "/home/bogner/eig/C36_modified.pdf",
                                        "/home/bogner/eig/C37_modified.pdf"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var actual = DocSysOperations.GetCustomPdfElement(eob);

            var expected = new EobXmlCustomPdfs()
            {
                Method = "APPEND",
                DuplexPrint = "Yes",
                Order = 1,
                Files = new CustomPdfFile[]
                {
                    new CustomPdfFile { File = "/home/bogner/eig/C36_modified.pdf"},
                    new CustomPdfFile { File = "/home/bogner/eig/C37_modified.pdf"},
                }
            };

            actual.Should().BeEquivalentTo(expected);
        }

        #endregion GetCustomPdfElement

        #region GetEobDocument

        [Theory]
        [InlineData("NONE", true, EobGenerateType.NoEobFromPayer)]
        [InlineData("NONE", false, EobGenerateType.NoEobFromPayer)]
        [InlineData("PDF", true, EobGenerateType.NoEobFromPayer)]
        [InlineData("PDF", false, EobGenerateType.PdfOnly)]
        [InlineData("DATA", true, EobGenerateType.NoEobFromPayer)]
        [InlineData("DATA", false, EobGenerateType.DataOnly)]
        [InlineData("DATA,PDF", true, EobGenerateType.NoEobFromPayer)]
        [InlineData("DATA,PDF", false, EobGenerateType.DataThenPdf)]
        [InlineData("PDF,DATA", true, EobGenerateType.NoEobFromPayer)]
        [InlineData("PDF,DATA", false, EobGenerateType.PdfThenData)]
        public void GetEobDocument_WithEobMethodAndNonRptFlag_ReturnsCorrectEobGenerateType(string eobMethod, bool noEobRptFlag, EobGenerateType expectedResult)
        {
            var act = _ops.GetEobDocument(eobMethod, noEobRptFlag);

            act.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("JUNK")]
        [InlineData("EMAIL")]
        [InlineData("DAT")]
        [InlineData("NA")]
        public void GetEobDocument_InvalidEobMethodAndNoEobRptFlagIsFalse_ReturnsEobGenerateTypeOfNone(string invalidEobMethod)
        {
            var act = _ops.GetEobDocument(invalidEobMethod, false);

            act.Should().Be(EobGenerateType.None);
        }

        #endregion GetEobDocument

        #region GetEobMethod

        [Theory]
        [InlineData("NONE", "NONE")]
        [InlineData("none", "NONE")]
        [InlineData("PDF,Data", "PDF,DATA")]
        [InlineData("Data,Pdf", "DATA,PDF")]
        [InlineData("Data", "DATA")]
        [InlineData("Pdf", "PDF")]
        [InlineData("Junk", "JUNK")]
        [InlineData("", "")]
        public void GetEobMethod_EobMethodInXmlIsSetAndHasEobMakerIsTrue_ReturnsCapitalizedEobMethod(string eobMethod, string expectedResult)
        {
            var eob = new Dictionary<string, dynamic> { { "eob_method", eobMethod } };

            var act = _ops.GetEobMethod(eob, true, true);

            act.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("NONE")]
        [InlineData("none")]
        [InlineData("PDF,Data")]
        [InlineData("Data,Pdf")]
        [InlineData("Data")]
        [InlineData("Pdf")]
        [InlineData("Junk")]
        public void GetEobMethod_EobMethodInXmlIsSetAndHasEobMakerIsFalseAndHasPdfIsFalse_AlwaysReturnsNONE(string eobMethod)
        {
            var eob = new Dictionary<string, dynamic> { { "eob_method", eobMethod } };
            var act = _ops.GetEobMethod(eob, false, false);

            act.Should().Be("NONE");
        }

        [Fact]
        public void GetEobMethod_EobMethodInXmlIsNotSetAndHasEobMakerIsFalseAndHasPdfIsFalse_AlwaysReturnsNONE()
        {
            var eob = new Dictionary<string, dynamic> { };
            var act = _ops.GetEobMethod(eob, false, false);

            act.Should().Be("NONE");
        }

        [Theory]
        [InlineData("NONE")]
        [InlineData("none")]
        [InlineData("PDF,Data")]
        [InlineData("Data,Pdf")]
        [InlineData("Data")]
        [InlineData("Pdf")]
        [InlineData("Junk")]
        public void GetEobMethod_EobMethodInXmlIsSetAndHasEobMakerIsFalseAndHasPdfIsTrue_AlwaysReturnsPDF(string eobMethod)
        {
            var eob = new Dictionary<string, dynamic> { { "eob_method", eobMethod } };
            var act = _ops.GetEobMethod(eob, false, true);

            act.Should().Be("PDF");
        }

        [Fact]
        public void GetEobMethod_EobMethodInXmlIsNotSetAndHasEobMakerIsFalseAndHasPdfIsTrue_AlwaysReturnsPDF()
        {
            var eob = new Dictionary<string, dynamic> { };
            var act = _ops.GetEobMethod(eob, false, true);

            act.Should().Be("PDF");
        }

        [Fact]
        public void GetEobMethod_EobMethodInXmlDoesNotExistAndHasEobMakerIsTrueAndHasPdfIsTrue_AlwaysReturnsPDF()
        {
            var eob = new Dictionary<string, dynamic> { };
            var act = _ops.GetEobMethod(eob, true, true);

            act.Should().Be("DATA");
        }

        #endregion GetEobMethod

        #region GetPacketDocumentOrder

        [Fact]
        public void GetPacketDocumentOrder_WhenCustomPdfIsNull_ShouldReturnCoverThenEob()
        {
            EobXmlCustomPdfs customPdf = null;

            var expected = new List<PacketIncludedDocumentType> { PacketIncludedDocumentType.Cover, PacketIncludedDocumentType.EOB };

            var actual = _ops.GetPacketDocumentOrder(customPdf);

            actual.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        }

        [Fact]
        public void GetPacketDocumentOrder_WhenCustomPdfIsAppend_ShouldReturnCoverThenEobThenCustomPdf()
        {
            EobXmlCustomPdfs customPdf = new EobXmlCustomPdfs()
            {
                Method = "APPEND",
                Order = null,
                Files = new CustomPdfFile[] { new CustomPdfFile { File = "C:\\test\test1.pdf" } }
            };

            var expected = new List<PacketIncludedDocumentType> { PacketIncludedDocumentType.Cover, PacketIncludedDocumentType.EOB, PacketIncludedDocumentType.CustomPdf };

            var actual = _ops.GetPacketDocumentOrder(customPdf);

            actual.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        }

        [Fact]
        public void GetPacketDocumentOrder_WhenCustomPdfIsReplaceAndOrderIs1_ShouldReturnCustomPdfThenEob()
        {
            EobXmlCustomPdfs customPdf = new EobXmlCustomPdfs()
            {
                Method = "REPLACE",
                Order = 1,
                Files = new CustomPdfFile[] { new CustomPdfFile { File = "C:\\test\test1.pdf" } }
            };

            var expected = new List<PacketIncludedDocumentType> { PacketIncludedDocumentType.CustomPdf, PacketIncludedDocumentType.EOB };

            var actual = _ops.GetPacketDocumentOrder(customPdf);

            actual.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        }

        [Fact]
        public void GetPacketDocumentOrder_WhenCustomPdfIsReplaceAndOrderIs2_ShouldReturnCoverThenCustomPdf()
        {
            EobXmlCustomPdfs customPdf = new EobXmlCustomPdfs()
            {
                Method = "REPLACE",
                Order = 2,
                Files = new CustomPdfFile[] { new CustomPdfFile { File = "C:\\test\test1.pdf" } }
            };

            var expected = new List<PacketIncludedDocumentType> { PacketIncludedDocumentType.Cover, PacketIncludedDocumentType.CustomPdf };

            var actual = _ops.GetPacketDocumentOrder(customPdf);

            actual.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        }

        #endregion GetPacketDocumentOrder

        #region GetDocumentManifest

        [Theory]
        [InlineData(EobGenerateType.None, false)]
        [InlineData(EobGenerateType.NoEobFromPayer, false)]
        [InlineData(EobGenerateType.DataOnly, true)]
        [InlineData(EobGenerateType.PdfOnly, true)]
        [InlineData(EobGenerateType.DataThenPdf, true)]
        [InlineData(EobGenerateType.PdfThenData, true)]
        public async Task GetDocumentManifest_GenerateEobIsTrue_CallsGetEobDocumentAndAddsEOBToPacketBasedOnResult(EobGenerateType getEobDocumentResult, bool shouldHaveEOB)
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly();
            value.GenerateEOB = true;

            _opsMock.Setup(x => x.GetEobDocument(It.IsAny<string>(), It.IsAny<bool>())).Returns(getEobDocumentResult);

            var actual = await _ops.GetDocumentManifest(value);

            _opsMock.Verify(x => x.GetEobDocument(It.IsAny<string>(), It.IsAny<bool>()), Times.Once());

            if (shouldHaveEOB)
            {
                actual.PacketDocuments.Where(x => x.DocumentsNeeded.Any(y => y == PacketIncludedDocumentType.EOB)).Should().HaveCount(actual.PacketDocuments.Count);
            }
            else
            {
                actual.PacketDocuments.Where(x => x.DocumentsNeeded.Any(y => y == PacketIncludedDocumentType.EOB)).Should().BeEmpty();
            }

            actual.EobGenerateType.Should().Be(getEobDocumentResult);
        }

        [Theory]
        [InlineData(true, "YES")]
        [InlineData(true, "NO")]
        [InlineData(false, "YES")]
        [InlineData(false, "NO")]
        public async Task GetDocumentManifest_WithAppendMethod_WillAddCustomPdfToPacketAndSetForceEobToEvenPagesBasedOnDuplexPrintSetting(bool hasEob, string duplexPrintSetting)
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly();

            value.EOBData.XML = $@"<eob_xml>
  <custom_pdfs>
    <method>APPEND</method>
    <duplex_print>{duplexPrintSetting}</duplex_print>
    <order>1</order>
    <files>
      <file>/home/bogner/eig/C36_modified.pdf</file>
    </files>
  </custom_pdfs>
</eob_xml>";

            value.GenerateEOB = hasEob;

            _opsMock.Setup(x => x.GetEobDocument(It.IsAny<string>(), It.IsAny<bool>())).Returns(EobGenerateType.DataOnly);

            var actual = await _ops.GetDocumentManifest(value);

            actual.PacketDocuments.Where(x => x.DocumentsNeeded.Any(y => y == PacketIncludedDocumentType.CustomPdf)).Should().HaveCount(actual.PacketDocuments.Count);

            actual.PacketDocuments.Where(x => x.ForceEobToEvenPages == (duplexPrintSetting == "YES")).Should().HaveCount(actual.PacketDocuments.Count);
        }

        [Theory]
        [InlineData(PayType.CLVCP, VPaygeDocumentType.VCard_Masked, VPaygeDocumentType.VCard_Clear)]
        [InlineData(PayType.CLEFT, VPaygeDocumentType.EFT, null)]
        [InlineData(PayType.CLSDA, VPaygeDocumentType.SDA, null)]
        [InlineData(PayType.CLNPF, VPaygeDocumentType.NoPay, null)]
        [InlineData(PayType.CLCHK, VPaygeDocumentType.Check, null)]
        [InlineData(PayType.CLCCK, VPaygeDocumentType.Client_Check, null)]
        [InlineData(PayType.CLCFT, VPaygeDocumentType.Client_EFT, null)]
        [InlineData(PayType.OPACH, VPaygeDocumentType.OptumAch, null)]
        [InlineData("RandomType", VPaygeDocumentType.Check, null)]
        public async Task GetDocumentManifest_WithCoversheetInPacketDocuments_WillSetCorrectDocumentTypeAndIncludeCoverSheetDocumentsToGenerate(string payType, VPaygeDocumentType documentType1, VPaygeDocumentType? documentType2)
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly();
            value.EOBData.XML = $@"<eob_xml>
  <custom_pdfs>
    <method>APPEND</method>
    <duplex_print>NO</duplex_print>
    <order>1</order>
    <files>
      <file>/home/bogner/eig/C36_modified.pdf</file>
    </files>
  </custom_pdfs>
</eob_xml>";

            value.StatusRecord.PAYTYPE = payType;

            _opsMock.Setup(x => x.GetEobDocument(It.IsAny<string>(), It.IsAny<bool>())).Returns(EobGenerateType.DataOnly);

            var actual = await _ops.GetDocumentManifest(value);

            var count = documentType2.HasValue ? 2 : 1;

            actual.PacketDocuments.Should().HaveCount(count);
            actual.CoverSheetDocuments.Should().HaveCount(count);

            actual.CoverSheetDocuments.Should().Contain(documentType1);
            actual.PacketDocuments[0].DocumentType.Should().Be(documentType1);

            if (documentType2.HasValue)
            {
                actual.CoverSheetDocuments.Should().Contain(documentType2.Value);
                actual.PacketDocuments[1].DocumentType.Should().Be(documentType2);
            }
        }

        [Theory]
        [InlineData(PayType.CLVCP, VPaygeDocumentType.VCard_Masked, VPaygeDocumentType.VCard_Clear)]
        [InlineData(PayType.CLEFT, VPaygeDocumentType.EFT, null)]
        [InlineData(PayType.CLSDA, VPaygeDocumentType.SDA, null)]
        [InlineData(PayType.CLNPF, VPaygeDocumentType.NoPay, null)]
        [InlineData(PayType.CLCHK, VPaygeDocumentType.Check, null)]
        [InlineData(PayType.CLCCK, VPaygeDocumentType.Client_Check, null)]
        [InlineData(PayType.CLCFT, VPaygeDocumentType.Client_EFT, null)]
        [InlineData(PayType.OPACH, VPaygeDocumentType.OptumAch, null)]
        [InlineData("RandomType", VPaygeDocumentType.Check, null)]
        public async Task GetDocumentManifest_WithCoversheetInPacketDocuments_WillSetCorrectDocumentTypeAndCoversheetsShouldBeEmpty(string payType, VPaygeDocumentType documentType1, VPaygeDocumentType? documentType2)
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly();
            value.EOBData.XML = $@"<eob_xml>
  <custom_pdfs>
    <method>REPLACE</method>
    <duplex_print>NO</duplex_print>
    <order>1</order>
    <files>
      <file>/home/bogner/eig/C36_modified.pdf</file>
    </files>
  </custom_pdfs>
</eob_xml>";

            value.StatusRecord.PAYTYPE = payType;

            _opsMock.Setup(x => x.GetEobDocument(It.IsAny<string>(), It.IsAny<bool>())).Returns(EobGenerateType.DataOnly);

            var actual = await _ops.GetDocumentManifest(value);

            var count = documentType2.HasValue ? 2 : 1;

            actual.PacketDocuments.Should().HaveCount(count);

            actual.CoverSheetDocuments.Should().BeEmpty();
            actual.PacketDocuments[0].DocumentType.Should().Be(documentType1);

            if (documentType2.HasValue)
            {
                actual.PacketDocuments[1].DocumentType.Should().Be(documentType2);
            }
        }

        [Fact]
        public async Task GetDocumentManifest_WithStatusRecordDistTypeSetToUnload_WillNotHaveCoverSheetsAndPacketsToGenerate()
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly();
            value.StatusRecord.DIST = DistType.Unload;

            _opsMock.Setup(x => x.GetEobDocument(It.IsAny<string>(), It.IsAny<bool>())).Returns(EobGenerateType.DataOnly);

            var actual = await _ops.GetDocumentManifest(value);

            actual.PacketDocuments.Should().HaveCount(0);
            actual.CoverSheetDocuments.Should().HaveCount(0);
        }

        #endregion GetDocumentManifest

        #region GenerateDoc

        [Fact]
        public async Task GenerateEobDoc_WithJustDocGen_CallsInternalGenerateEobDoc()
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly();
            var expected = new List<EobGeneratedFile>();
            _docSysSettings.AllowUnicode = false;

            _eobDocumentGeneratorService.Setup(x => x.GenerateEobDocuments(EobGenerateType.PdfOnly, It.IsAny<DocGenParamInternal>())).ReturnsAsync(expected);

            var actual = await _ops.GenerateEobDoc(value);

            _eobDocumentGeneratorService.Verify(x => x.GenerateEobDocuments(EobGenerateType.PdfOnly, It.IsAny<DocGenParamInternal>()));
            _opsMock.Verify(x => x.CleanUnicodeFromEobXml(value), Times.Once());

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task GenerateEobDoc_WithJustDocGen_CallsInternalGenerateEobDoc_NoUnicode()
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly();
            var expected = new List<EobGeneratedFile>();
            _docSysSettings.AllowUnicode = true;

            _eobDocumentGeneratorService.Setup(x => x.GenerateEobDocuments(EobGenerateType.PdfOnly, It.IsAny<DocGenParamInternal>())).ReturnsAsync(expected);

            var actual = await _ops.GenerateEobDoc(value);

            _eobDocumentGeneratorService.Verify(x => x.GenerateEobDocuments(EobGenerateType.PdfOnly, It.IsAny<DocGenParamInternal>()));
            _opsMock.Verify(x => x.CleanUnicodeFromEobXml(value), Times.Never());

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void GenerateEobDoc_DocGenParamIsNull_ThrowsArgumentNullException()
        {
            Func<Task> act = async () => await _ops.GenerateEobDoc(null);

            act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("docGenParam");
        }

        [Fact]
        public void GenerateEobDoc_DocGenParamConfigurationIsNull_ThrowsArgumentException()
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly();
            value.Configuration = null;
            Func<Task> act = async () => await _ops.GenerateEobDoc(value);

            var exception = act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Configuration cannot be null.")
                .WithParameterName("docGenParam");
        }

        [Fact]
        public void GenerateEobDoc_DocGenParamStatusRecordIsNull_ThrowsArgumentException()
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly();
            value.StatusRecord = null;
            Func<Task> act = async () => await _ops.GenerateEobDoc(value);

            var exception = act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("StatusRecord cannot be null.")
                .WithParameterName("docGenParam");
        }

        [Fact]
        public void GenerateEobDoc_DocGenParamOtbRecordIsNull_ThrowsArgumentException()
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly();
            value.OtbRecord = null;
            Func<Task> act = async () => await _ops.GenerateEobDoc(value);

            var exception = act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("OtbRecord cannot be null.")
                .WithParameterName("docGenParam");
        }

        [Fact]
        public void GenerateEobDoc_DocGenParamEOBDataIsNull_ThrowsArgumentException()
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly();
            value.EOBData = null;
            Func<Task> act = async () => await _ops.GenerateEobDoc(value);

            var exception = act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("EOBData cannot be null.")
                .WithParameterName("docGenParam");
        }

        [Fact]
        public void GenerateEobDoc_DocGenParamEOBData_Name_IsNull_ThrowsArgumentException()
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly();
            value.EOBData.Name = null;
            Func<Task> act = async () => await _ops.GenerateEobDoc(value);

            var exception = act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("EOBData.Name cannot be null or an empty string")
                .WithParameterName("docGenParam");
        }

        [Fact]
        public void GenerateEobDoc_DocGenParamEOBData_Name_IsEmptyString_ThrowsArgumentException()
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly();
            value.EOBData.Name = "";
            Func<Task> act = async () => await _ops.GenerateEobDoc(value);

            var exception = act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("EOBData.Name cannot be null or an empty string")
                .WithParameterName("docGenParam");
        }

        [Fact]
        public void GenerateEobDoc_DocGenParamEOBData_Name_IsWhiteSpace_ThrowsArgumentException()
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly();
            value.EOBData.Name = "  ";
            Func<Task> act = async () => await _ops.GenerateEobDoc(value);

            var exception = act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("EOBData.Name cannot be null or an empty string")
                .WithParameterName("docGenParam");
        }

        [Fact]
        public void GenerateEobDoc_DocGenParamEOBData_XML_IsNull_ThrowsArgumentException()
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly();
            value.EOBData.XML = null;
            Func<Task> act = async () => await _ops.GenerateEobDoc(value);

            var exception = act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("EOBData.XML cannot be null or an empty string")
                .WithParameterName("docGenParam");
        }

        [Fact]
        public void GenerateEobDoc_DocGenParamEOBData_XML_IsEmptyString_ThrowsArgumentException()
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly();
            value.EOBData.XML = "";
            Func<Task> act = async () => await _ops.GenerateEobDoc(value);

            var exception = act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("EOBData.XML cannot be null or an empty string")
                .WithParameterName("docGenParam");
        }

        [Fact]
        public void GenerateEobDoc_DocGenParamEOBData_XML_IsWhiteSpace_ThrowsArgumentException()
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly();
            value.EOBData.XML = "  ";
            Func<Task> act = async () => await _ops.GenerateEobDoc(value);

            var exception = act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("EOBData.XML cannot be null or an empty string")
                .WithParameterName("docGenParam");
        }

        #endregion GenerateDoc

        [Theory]
        [InlineData("ÀÁÂÃÄÅÆ", "AAAAAAAE")]
        [InlineData("ÈÉÊË", "EEEE")]
        [InlineData("ÌÍÎÏ", "IIII")]
        [InlineData("ÒÓÔÕÖ", "OOOOO")]
        [InlineData("ÙÚÛÜ", "UUUU")]
        [InlineData("àáâãäåæ", "aaaaaaae")]
        [InlineData("èéêë", "eeee")]
        [InlineData("ìíîï", "iiii")]
        [InlineData("òóôõö", "ooooo")]
        [InlineData("ùúûü", "uuuu")]
        [InlineData("Ññ", "Nn")]
        [InlineData("’‘–”“…‚—", @"''-""""...,--")]
        public void CleanUnicodeFromEobXml_DocGenParamXmlWithUnicode_WillConvertToAscii(string unicode, string ascii)
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly();

            value.EOBData.XML = GenerateDocGenParam.GetEobXml(new List<string> { unicode }, EobGenerateType.NoEobFromPayer);

            var expected = GenerateDocGenParam.GetEobXml(new List<string> { ascii }, EobGenerateType.NoEobFromPayer);
            _ops.CleanUnicodeFromEobXml(value);

            value.EOBData.XML.Should().Be(expected);
        }
    }
}
