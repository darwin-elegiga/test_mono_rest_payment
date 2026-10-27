using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Configs;
using VPay.DocSys.Generation.Tests.TestModels;
using VPay.DocSys.Pdf.Tools;
using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class PacketDocumentGeneratorServiceTests
    {
        private readonly IPacketDocumentGeneratorService SUT;

        private readonly Mock<IPdfTools> _pdfTools;
        private readonly MoqFileSystem _fileSystem;
        private readonly DocSysSettings _docSysSettings;

        public PacketDocumentGeneratorServiceTests()
        {
            _docSysSettings = new DocSysSettings
            {
                ResultPath = "C:\\test",
                WhiteBoxPdf = "C:\\test\\whiteBox.pdf"
            };

            _fileSystem = new MoqFileSystem(new Dictionary<string, MockFileData>()
            {
                { _docSysSettings.WhiteBoxPdf, new MockFileData(new byte[] { 0x12, 0x34, 0x56, 0xd2 }) }
            });

            _pdfTools = new Mock<IPdfTools>();

            SUT = new PacketDocumentGeneratorService(
                _docSysSettings,
                _pdfTools.Object,
                _fileSystem,
                TestLogger.Create<PacketDocumentGeneratorService>());
        }

        [Fact]
        public async Task GeneratePacketDocInternal_CannotFindEobFile_ShouldThrowFileNotFoundError()
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly().ToDocGenParamInternal(_docSysSettings.ResultPath, _fileSystem, false);
            var packetDoc = new PacketDocument
            {
                ForceEobToEvenPages = false,
                DocumentsNeeded = new List<PacketIncludedDocumentType>
                {
                    PacketIncludedDocumentType.EOB
                }
            };

            var expectedEobFileName = value.EobXmlFilePath.Replace(".eob.xml", ".plain_eob.pdf");

            Func<Task> act = async () => await SUT.GeneratePacketDocuments(new[] { packetDoc }, value);

            var exception = await act.Should().ThrowAsync<FileNotFoundException>()
                .WithMessage("Could not locate EOB file");

            exception.Which.FileName.Should().Be(expectedEobFileName);
        }

        [Fact]
        public async Task GeneratePacketDocInternal_IncludeFoundEobDocument_ShouldIncludeEobDocumentInMerge()
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly().ToDocGenParamInternal(_docSysSettings.ResultPath, _fileSystem, false);
            var packetDoc = new PacketDocument
            {
                ForceEobToEvenPages = false,
                DocumentsNeeded = new List<PacketIncludedDocumentType>
                {
                    PacketIncludedDocumentType.EOB
                },
                DocumentType = VPaygeDocumentType.Check
            };

            var expectedEobFileName = value.EobXmlFilePath.Replace(".eob.xml", ".plain_eob.pdf");
            var actualMergeArgs = new ArgumentCaptor<List<string>>();

            _fileSystem.FileMoq.Setup(x => x.Exists(expectedEobFileName)).Returns(true);
            _pdfTools.Setup(x => x.MergeAsync(It.IsAny<string>(), actualMergeArgs.Capture())).ReturnsAsync(true);

            await SUT.GeneratePacketDocuments(new[] { packetDoc }, value);

            actualMergeArgs.Value.Should().Contain(expectedEobFileName);
        }

        [Theory]
        [InlineData(VPaygeDocumentType.Check)]
        [InlineData(VPaygeDocumentType.EFT)]
        [InlineData(VPaygeDocumentType.NoPay)]
        [InlineData(VPaygeDocumentType.VCard_Clear)]
        [InlineData(VPaygeDocumentType.VCard_Masked)]
        [InlineData(VPaygeDocumentType.Client_Check)]
        [InlineData(VPaygeDocumentType.Client_EFT)]
        [InlineData(VPaygeDocumentType.SDA)]
        [InlineData(VPaygeDocumentType.OptumAch)]
        public async Task GeneratePacketDocInternal_CannotFindVPaygeCover_ShouldThrowFileNotFoundError(VPaygeDocumentType documentType)
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly().ToDocGenParamInternal(_docSysSettings.ResultPath, _fileSystem, false);
            var packetDoc = new PacketDocument
            {
                ForceEobToEvenPages = false,
                DocumentsNeeded = new List<PacketIncludedDocumentType>
                {
                    PacketIncludedDocumentType.Cover
                },
                DocumentType = documentType
            };

            var expectedFileName = value.GetVPaygeFileName(documentType, value.DocGenParam.IsRedCard);

            Func<Task> act = async () => await SUT.GeneratePacketDocuments(new[] { packetDoc }, value);

            var exception = await act.Should().ThrowAsync<FileNotFoundException>()
                .WithMessage("Could not locate VPayge Cover file");

            exception.Which.FileName.Should().Be(expectedFileName);
        }

        [Theory]
        [InlineData(VPaygeDocumentType.Check)]
        [InlineData(VPaygeDocumentType.EFT)]
        [InlineData(VPaygeDocumentType.NoPay)]
        [InlineData(VPaygeDocumentType.VCard_Clear)]
        [InlineData(VPaygeDocumentType.VCard_Masked)]
        [InlineData(VPaygeDocumentType.Client_Check)]
        [InlineData(VPaygeDocumentType.Client_EFT)]
        [InlineData(VPaygeDocumentType.SDA)]
        [InlineData(VPaygeDocumentType.OptumAch)]
        public async Task GeneratePacketDocInternal_IncludeVPaygeCover_ShouldIncludeVPaygeCoverInMerge(VPaygeDocumentType documentType)
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly().ToDocGenParamInternal(_docSysSettings.ResultPath, _fileSystem, false);
            var packetDoc = new PacketDocument
            {
                ForceEobToEvenPages = false,
                DocumentsNeeded = new List<PacketIncludedDocumentType>
                {
                    PacketIncludedDocumentType.Cover
                },
                DocumentType = documentType
            };

            var expectedFileName = value.GetVPaygeFileName(documentType, value.DocGenParam.IsRedCard);
            var actualMergeArgs = new ArgumentCaptor<List<string>>();

            _fileSystem.FileMoq.Setup(x => x.Exists(expectedFileName)).Returns(true);
            _pdfTools.Setup(x => x.MergeAsync(It.IsAny<string>(), actualMergeArgs.Capture())).ReturnsAsync(true);

            await SUT.GeneratePacketDocuments(new[] { packetDoc }, value);

            actualMergeArgs.Value.Should().Contain(expectedFileName);
        }

        [Theory]
        [ClassData(typeof(GeneratePacketCustomPdfNotFoundData))]
        public async Task GeneratePacketDocInternal_CannotFindCustomPdfs_ShouldThrowFileNotFoundError(TestCustomPdfFileList pdfFile)
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly().ToDocGenParamInternal(_docSysSettings.ResultPath, _fileSystem, false);
            var packetDoc = new PacketDocument
            {
                ForceEobToEvenPages = false,
                CustomPdfs = new List<string>(),
                DocumentsNeeded = new List<PacketIncludedDocumentType>
                {
                    PacketIncludedDocumentType.CustomPdf
                },
                DocumentType = VPaygeDocumentType.Check
            };

            var firstFailure = "";

            foreach (var pdf in pdfFile)
            {
                packetDoc.CustomPdfs.Add(pdf.Name);

                _fileSystem.FileMoq.Setup(x => x.Exists(pdf.Name)).Returns(pdf.Exists);

                if (string.IsNullOrWhiteSpace(firstFailure) && !pdf.Exists)
                {
                    firstFailure = pdf.Name;
                }
            }

            var actualMergeArgs = new ArgumentCaptor<List<string>>();

            _pdfTools.Setup(x => x.MergeAsync(It.IsAny<string>(), actualMergeArgs.Capture())).ReturnsAsync(true);

            Func<Task> act = async () => await SUT.GeneratePacketDocuments(new[] { packetDoc }, value);

            var exception = await act.Should().ThrowAsync<FileNotFoundException>()
                .WithMessage("Could not locate CustomPdf file");

            exception.Which.FileName.Should().Be(firstFailure);
        }

        [Fact]
        public async Task GeneratePacketDocInternal_IncludeCustomPdfs_ShouldIncludePdfsInMerge()
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly().ToDocGenParamInternal(_docSysSettings.ResultPath, _fileSystem, false);
            var packetDoc = new PacketDocument
            {
                ForceEobToEvenPages = false,
                CustomPdfs = new List<string>()
                {
                    "C://test//test1.pdf",
                    "C://test//test2.pdf",
                },
                DocumentsNeeded = new List<PacketIncludedDocumentType>
                {
                    PacketIncludedDocumentType.CustomPdf
                },
                DocumentType = VPaygeDocumentType.Check
            };

            var actualMergeArgs = new ArgumentCaptor<List<string>>();

            foreach (var pdf in packetDoc.CustomPdfs)
            {
                _fileSystem.FileMoq.Setup(x => x.Exists(pdf)).Returns(true);
            }
            _pdfTools.Setup(x => x.MergeAsync(It.IsAny<string>(), actualMergeArgs.Capture())).ReturnsAsync(true);

            _ = await SUT.GeneratePacketDocuments(new[] { packetDoc }, value);

            actualMergeArgs.Value.Should().ContainInOrder(packetDoc.CustomPdfs);
        }

        [Theory]
        [InlineData(PacketIncludedDocumentType.Cover, PacketIncludedDocumentType.CustomPdf, null)]
        [InlineData(PacketIncludedDocumentType.Cover, PacketIncludedDocumentType.EOB, null)]
        [InlineData(PacketIncludedDocumentType.EOB, PacketIncludedDocumentType.CustomPdf, null)]
        [InlineData(PacketIncludedDocumentType.EOB, PacketIncludedDocumentType.Cover, null)]
        [InlineData(PacketIncludedDocumentType.CustomPdf, PacketIncludedDocumentType.EOB, null)]
        [InlineData(PacketIncludedDocumentType.CustomPdf, PacketIncludedDocumentType.Cover, null)]
        [InlineData(PacketIncludedDocumentType.Cover, PacketIncludedDocumentType.CustomPdf, PacketIncludedDocumentType.EOB)]
        [InlineData(PacketIncludedDocumentType.Cover, PacketIncludedDocumentType.EOB, PacketIncludedDocumentType.CustomPdf)]
        [InlineData(PacketIncludedDocumentType.EOB, PacketIncludedDocumentType.CustomPdf, PacketIncludedDocumentType.Cover)]
        [InlineData(PacketIncludedDocumentType.EOB, PacketIncludedDocumentType.Cover, PacketIncludedDocumentType.CustomPdf)]
        [InlineData(PacketIncludedDocumentType.CustomPdf, PacketIncludedDocumentType.EOB, PacketIncludedDocumentType.Cover)]
        [InlineData(PacketIncludedDocumentType.CustomPdf, PacketIncludedDocumentType.Cover, PacketIncludedDocumentType.EOB)]
        public async Task GeneratePacketDocInternal_WithDocumentsNeededInOrder_ShouldIncludeDocumentsInMergeInThatOrder(PacketIncludedDocumentType firstItem, PacketIncludedDocumentType secondItem, PacketIncludedDocumentType? thirdItem)
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly().ToDocGenParamInternal(_docSysSettings.ResultPath, _fileSystem, false);
            var packetDoc = new PacketDocument
            {
                ForceEobToEvenPages = false,
                CustomPdfs = new List<string>()
                {
                    "C://test//test1.pdf",
                    "C://test//test2.pdf",
                },
                DocumentsNeeded = new List<PacketIncludedDocumentType>
                {
                    firstItem,
                    secondItem
                },
                DocumentType = VPaygeDocumentType.Check
            };

            if (thirdItem.HasValue)
            {
                packetDoc.DocumentsNeeded.Add(thirdItem.Value);
            }

            var expectedEobFileName = value.EobXmlFilePath.Replace(".eob.xml", ".plain_eob.pdf");
            var expectedVPaygeFileName = value.GetVPaygeFileName(packetDoc.DocumentType, value.DocGenParam.IsRedCard);
            var expectedMergeOrder = new List<string>();

            foreach (var type in packetDoc.DocumentsNeeded)
            {
                switch (type)
                {
                    case PacketIncludedDocumentType.Cover:
                        expectedMergeOrder.Add(expectedVPaygeFileName);
                        break;

                    case PacketIncludedDocumentType.EOB:
                        expectedMergeOrder.Add(expectedEobFileName);
                        break;

                    case PacketIncludedDocumentType.CustomPdf:
                        expectedMergeOrder.AddRange(packetDoc.CustomPdfs);
                        break;
                }
            }

            var actualMergeArgs = new ArgumentCaptor<List<string>>();

            _fileSystem.FileMoq.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
            _pdfTools.Setup(x => x.MergeAsync(It.IsAny<string>(), actualMergeArgs.Capture())).ReturnsAsync(true);

            _ = await SUT.GeneratePacketDocuments(new[] { packetDoc }, value);

            actualMergeArgs.Value.Should().ContainInOrder(expectedMergeOrder).And.HaveCount(expectedMergeOrder.Count);
        }

        [Fact]
        public async Task GeneratePacketDocInternal_ForceEobToEvenPagesWithOddEobPages_ShouldIncludeBlankPdf()
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly().ToDocGenParamInternal(_docSysSettings.ResultPath, _fileSystem, false);
            var eobPageCount = 1;

            var packetDoc = new PacketDocument
            {
                ForceEobToEvenPages = true,
                DocumentsNeeded = new List<PacketIncludedDocumentType>
                {
                    PacketIncludedDocumentType.EOB
                },
                DocumentType = VPaygeDocumentType.Check
            };

            var expectedEobFileName = value.EobXmlFilePath.Replace(".eob.xml", ".plain_eob.pdf");

            _fileSystem.FileMoq.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
            _pdfTools.Setup(x => x.GetNumberOfPagesAsync(expectedEobFileName)).ReturnsAsync(eobPageCount);

            var actualMergeArgs = new ArgumentCaptor<List<string>>();
            _pdfTools.Setup(x => x.MergeAsync(It.IsAny<string>(), actualMergeArgs.Capture())).ReturnsAsync(true);

            _ = await SUT.GeneratePacketDocuments(new[] { packetDoc }, value);

            actualMergeArgs.Value.Should().Contain(_docSysSettings.WhiteBoxPdf);
        }

        [Fact]
        public async Task GeneratePacketDocInternal_ForceEobToEvenPagesWithEvenEobPages_ShouldNotIncludeBlankPdf()
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly().ToDocGenParamInternal(_docSysSettings.ResultPath, _fileSystem, false);
            var eobPageCount = 2;

            var packetDoc = new PacketDocument
            {
                ForceEobToEvenPages = true,
                DocumentsNeeded = new List<PacketIncludedDocumentType>
                {
                    PacketIncludedDocumentType.EOB
                },
                DocumentType = VPaygeDocumentType.Check
            };

            var expectedEobFileName = value.EobXmlFilePath.Replace(".eob.xml", ".plain_eob.pdf");

            _fileSystem.FileMoq.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
            _pdfTools.Setup(x => x.GetNumberOfPagesAsync(expectedEobFileName)).ReturnsAsync(eobPageCount);

            var actualMergeArgs = new ArgumentCaptor<List<string>>();
            _pdfTools.Setup(x => x.MergeAsync(It.IsAny<string>(), actualMergeArgs.Capture())).ReturnsAsync(true);

            _ = await SUT.GeneratePacketDocuments(new[] { packetDoc }, value);

            actualMergeArgs.Value.Should().NotContain(_docSysSettings.WhiteBoxPdf);
        }

        [Theory]
        [MemberData(nameof(VpageTestData))]
        public async Task GetPacketInfo_ForPacketDocumentTypeAndWithHasEOB_ShouldSetToExpectedFileDetailTypeAndFileNameExtensionAndDescriptionAndIncludedVPaygeDocumentType(VPaygeDocumentType documentType, bool hasEobDocument, FileDetailType expectedFileDetailType, string expectedDescription, string expectedFileExtension)
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly().ToDocGenParamInternal(_docSysSettings.ResultPath, _fileSystem, false);

            var packet = new PacketDocument
            {
                DocumentType = documentType,
                DocumentsNeeded = hasEobDocument ? new List<PacketIncludedDocumentType> { PacketIncludedDocumentType.EOB } : new List<PacketIncludedDocumentType>()
            };

            var expected = new PacketGeneratedFile()
            {
                FileDetailType = expectedFileDetailType,
                Description = expectedDescription,
                TransactionId = value.DocGenParam.StatusRecord.TXID,
                FileName = value.GetSaveFileName(expectedFileExtension),
                IncludedVPaygeDocumentType = documentType
            };

            _fileSystem.File.WriteAllText(value.EobXmlFilePath.Replace(".eob.xml", Storage.Constants.FileExtensions.EobPdf), "");

            _pdfTools.Setup(x => x.MergeAsync(It.IsAny<string>(), It.IsAny<IList<string>>()))
                .ReturnsAsync(true);

            var actual = await SUT.GeneratePacketDocuments(new[] { packet }, value);

            _pdfTools.VerifyAll();

            actual.First().Should().BeEquivalentTo(expected);
        }

        [Theory]
        [InlineData(VPaygeDocumentType.VCard_Clear)]
        [InlineData(VPaygeDocumentType.EFT)]
        [InlineData(VPaygeDocumentType.SDA)]
        [InlineData(VPaygeDocumentType.NoPay)]
        [InlineData(VPaygeDocumentType.Check)]
        [InlineData(VPaygeDocumentType.OptumAch)]
        public async Task GetPacketInfo_ForPacketDocumentTypeAndIsRedCard_ShouldContainHasCoverVPaygeDocumentType(VPaygeDocumentType documentType)
        {
            var value = GenerateDocGenParam.GetEOBPdfOnly(true).ToDocGenParamInternal(_docSysSettings.ResultPath, _fileSystem, false);

            var packet = new PacketDocument
            {
                DocumentType = documentType,
                DocumentsNeeded = new List<PacketIncludedDocumentType> { PacketIncludedDocumentType.Cover }
            };

            _fileSystem.File.WriteAllText(value.EobXmlFilePath.Replace(".eob.xml", Storage.Constants.FileExtensions.EobPdf), "");
            _fileSystem.FileMoq.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);

            var actual = await SUT.GeneratePacketDocuments(new[] { packet }, value);

            actual.Should().Contain(_ => _.IncludedVPaygeDocumentType == VPaygeDocumentType.Has_Cover);
        }

        public static IEnumerable<object[]> VpageTestData
        {
            get
            {
                yield return new object[] { VPaygeDocumentType.VCard_Clear, true, FileDetailType.Limited, "Clear-PAN + EOB", ".CLEAR_eob.pdf", };
                yield return new object[] { VPaygeDocumentType.VCard_Clear, false, FileDetailType.Limited, "Clear-PAN", ".CLEAR_eob.pdf", };
                yield return new object[] { VPaygeDocumentType.VCard_Masked, true, FileDetailType.Pdf, "Masked-PAN + EOB", ".MASKED_eob.pdf", };
                yield return new object[] { VPaygeDocumentType.VCard_Masked, false, FileDetailType.Pdf, "Masked-PAN", ".MASKED_eob.pdf", };
                yield return new object[] { VPaygeDocumentType.NoPay, true, FileDetailType.Pdf, "NPF + EOB", ".NPF_cover_eob.pdf", };
                yield return new object[] { VPaygeDocumentType.NoPay, false, FileDetailType.Pdf, "NPF", ".NPF_cover_eob.pdf", };
                yield return new object[] { VPaygeDocumentType.EFT, true, FileDetailType.Pdf, "EFT + EOB", ".EFT_cover_eob.pdf", };
                yield return new object[] { VPaygeDocumentType.EFT, false, FileDetailType.Pdf, "EFT", ".EFT_cover_eob.pdf", };
                yield return new object[] { VPaygeDocumentType.Check, true, FileDetailType.Pdf, "Check + EOB", ".CHECK_cover_eob.pdf", };
                yield return new object[] { VPaygeDocumentType.Check, false, FileDetailType.Pdf, "Check", ".CHECK_cover_eob.pdf", };
                yield return new object[] { VPaygeDocumentType.Client_Check, true, FileDetailType.Pdf, "Check + EOB", ".CHECK_cover_eob.pdf", };
                yield return new object[] { VPaygeDocumentType.Client_Check, false, FileDetailType.Pdf, "Check", ".CHECK_cover_eob.pdf", };
                yield return new object[] { VPaygeDocumentType.Client_EFT, true, FileDetailType.Pdf, "EFT + EOB", ".EFT_cover_eob.pdf", };
                yield return new object[] { VPaygeDocumentType.Client_EFT, false, FileDetailType.Pdf, "EFT", ".EFT_cover_eob.pdf", };
                yield return new object[] { VPaygeDocumentType.SDA, true, FileDetailType.Pdf, "EFT + EOB", ".EFT_cover_eob.pdf", };
                yield return new object[] { VPaygeDocumentType.SDA, false, FileDetailType.Pdf, "EFT", ".EFT_cover_eob.pdf", };
                yield return new object[] { VPaygeDocumentType.OptumAch, true, FileDetailType.Pdf, "EFT + EOB", ".EFT_cover_eob.pdf", };
                yield return new object[] { VPaygeDocumentType.OptumAch, false, FileDetailType.Pdf, "EFT", ".EFT_cover_eob.pdf", };
            }
        }
    }
}
