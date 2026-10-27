using FluentAssertions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Moq;

using System;
using System.Threading.Tasks;

using VPay.DocSys.Generation.Api.Controllers;
using VPay.DocSys.Generation.Common;
using Xunit;

namespace VPay.DocSys.Generation.Api.Tests
{
    public class DropToPrintControllerTests : IDisposable
    {
        private readonly ILogger<DropToPrintController> _logger;

        public DropToPrintControllerTests()
        {
            _logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<DropToPrintController>();
        }

        public void Dispose()
        {
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(-1)]
        public async Task DropToPrint_ReturnsOkResult_WithStatusCodeResponse(int pythonStatusCode)
        {
            //Arrange
            var param = "/path/file.sts";
            var d2pServiceMock = new Mock<IDropToPrintService>();
            d2pServiceMock.Setup(x => x.CallDropToPrintPython(param)).Returns(Task.FromResult(pythonStatusCode)).Verifiable();
            var controller = new DropToPrintController(d2pServiceMock.Object, _logger);

            //Act
            var result = await controller.DropToPrint(param);

            //Assert
            d2pServiceMock.VerifyAll();
            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().BeEquivalentTo(new { PythonStatusCode = pythonStatusCode });
        }        
    }
}
