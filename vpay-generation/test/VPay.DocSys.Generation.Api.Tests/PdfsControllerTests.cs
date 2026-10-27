using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Api.Controllers;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Pdf.Tools;
using Xunit;

namespace VPay.DocSys.Generation.Api.Tests
{
    public class PdfsControllerTests : IDisposable
    {
        private readonly Mock<IPdfTools> _mockPdfTools;
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly Mock<ITiffOperations> _mockTiffOperations;
        private readonly ILogger<PdfsController> _logger;
        private readonly PdfsController _controller;

        public PdfsControllerTests()
        {
            _mockPdfTools = new Mock<IPdfTools>();
            _mockFileSystem = new Mock<IFileSystem>();
            _mockTiffOperations = new Mock<ITiffOperations>();
            _logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<PdfsController>();

            _controller = new PdfsController(_mockPdfTools.Object, _mockFileSystem.Object, _mockTiffOperations.Object, _logger);
        }

        public void Dispose()
        {
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task PostMerge_ReturnsOkResult_WithPdfResponse(bool generateSuccessFul)
        {
            //Arrange
            var param = new MergePdfParam()
            {
                OutputFileName = "output.pdf",
                Files = new List<string>()
                {
                    "file1.pdf",
                    "file2.pdf",
                    "file3.pdf"
                }
            };
            
            _mockPdfTools.Setup(x => x.MergeAsync(param.OutputFileName, param.Files)).Returns(Task.FromResult(generateSuccessFul)).Verifiable();

            //Act
            var result = await _controller.PostMerge(param);

            //Assert
            _mockPdfTools.VerifyAll();
            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().BeOfType<GenericPdfResponse>()
                .Which.Should().BeEquivalentTo(new GenericPdfResponse { IsSuccessful = generateSuccessFul, OutputFileName = param.OutputFileName });
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(true, null)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        [InlineData(false, null)]
        public async Task PostStamp_ReturnsOkResult_WithPdfResponse(bool generateSuccessFul, bool? useAllPagesOfStampFile)
        {
            //Arrange
            var param = new StampPdfParam()
            {
                OutputFileName = "output.pdf",
                StampFileName = "stamp.pdf",
                FileName = "fileName",
                UseAllPagesOfStampFile = useAllPagesOfStampFile
            };

            _mockPdfTools.Setup(x => x.StampAsync(param.OutputFileName, param.FileName, param.StampFileName)).Returns(Task.FromResult(generateSuccessFul)).Verifiable();

            //Act
            var result = await _controller.PostStamp(param);

            //Assert
            _mockPdfTools.VerifyAll();
            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().BeOfType<GenericPdfResponse>()
                .Which.Should().BeEquivalentTo(new GenericPdfResponse { IsSuccessful = generateSuccessFul, OutputFileName = param.OutputFileName });
        }

    }
}
