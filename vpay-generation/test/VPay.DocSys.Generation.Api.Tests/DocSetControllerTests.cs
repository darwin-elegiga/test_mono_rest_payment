using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Api.Controllers;
using VPay.DocSys.Generation.Common;
using Xunit;

namespace VPay.DocSys.Generation.Api.Tests
{
    public class DocSetControllerTests : IDisposable
    {
        private readonly Mock<IDocSysOperations> _docSysOperationsMock;
        private readonly DocSetsController _sut;

        public DocSetControllerTests()
        {
            _docSysOperationsMock = new Mock<IDocSysOperations>();
            _sut = new DocSetsController(_docSysOperationsMock.Object);
        }

        public void Dispose()
        {
            _docSysOperationsMock.VerifyAll();
        }

        [Fact]
        public async Task PostBuildGenerationSet_WithEntity_ReturnsOkWithDocGenManifest()
        {
            var entity = new DocGenParam();

            var expected = new DocGenManifest()
            {
                EobGenerateType = EobGenerateType.DataOnly,
                CoverSheetDocuments = new List<VPaygeDocumentType> { VPaygeDocumentType.Check },
                PacketDocuments = new List<PacketDocument>()
                {
                    new PacketDocument { ForceEobToEvenPages = true, CustomPdfs = null, DocumentsNeeded = new List<PacketIncludedDocumentType> { PacketIncludedDocumentType.Cover}, DocumentType = VPaygeDocumentType.Check}
                }
            };

            _docSysOperationsMock.Setup(x => x.GetDocumentManifest(entity)).ReturnsAsync(expected);

            var actual = await _sut.PostBuildGenerationSet(entity);

            _docSysOperationsMock.Verify(x => x.GetDocumentManifest(entity), Times.Once);

            actual.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().BeOfType<DocGenManifest>()
                .Which.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task PostGenerateEOB_WithValidResults_ReturnsOkWithEobGeneratedFile()
        {
            var entity = new DocGenParam();

            var expected = new List<EobGeneratedFile>
            {
                new EobGeneratedFile()
                {
                    EobGenerateType = EobGenerateType.DataOnly,
                    FileName = "Test.pdf",
                    FileDetailType = FileDetailType.Pdf,
                    Description = "This is a description",
                    TransactionId = "txid"
                }
            };

            _docSysOperationsMock.Setup(x => x.GenerateEobDoc(entity)).ReturnsAsync(expected);

            var actual = await _sut.PostGenerateEOB(entity);

            _docSysOperationsMock.Verify(x => x.GenerateEobDoc(entity), Times.Once);

            actual.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().BeOfType<List<EobGeneratedFile>>()
                .Which.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task PostGenerateEOB_WithInvalidResults_ReturnsBadRequest()
        {
            var entity = new DocGenParam();

            _docSysOperationsMock.Setup(x => x.GenerateEobDoc(entity)).ReturnsAsync(default(List<EobGeneratedFile>));

            var actual = await _sut.PostGenerateEOB(entity);

            _docSysOperationsMock.Verify(x => x.GenerateEobDoc(entity), Times.Once);

            actual.Should().BeOfType<BadRequestResult>();
        }

    }
}
