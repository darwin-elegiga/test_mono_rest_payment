using System;
using System.Threading;
using MediatR;
using Moq;
using Newtonsoft.Json;
using VPay.Optum.PaymentRouting.Api.Controllers;
using VPay.Optum.PaymentRouting.Application.About.Queries;
using Xunit;

namespace VPay.Optum.PaymentRouting.Api.Tests.Controllers
{
    public class AboutControllerFacts
    {
        private readonly AboutController SUT;
        private readonly Mock<IMediator> _mediator = new Mock<IMediator>();

        public AboutControllerFacts()
        {
            SUT = new AboutController(_mediator.Object);
        }

        [Fact]
        public void Should_Return_200OK()
        {
            // Arrange
            var expected = new GetAboutInfo.AboutInfo
            {
                Application = "Unit Test",
                Environment = "Unit Test",
                DataCenter = "Unit Test",
                K8sNodeName = "Unit Test",
                K8sPodName = "Unit Test",
                ReleaseTag = "Unit Test",
                ReleaseCommitSHA = "Unit Test",
                ImageBuildTime = DateTime.Now,
                DeploymentTime = DateTime.Now
            };

            _mediator.Setup(x => x.Send(It.IsAny<GetAboutInfo.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected)
                .Verifiable();

            // Act
            var result = SUT.GetDetail(default).Result;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(JsonConvert.SerializeObject(expected), JsonConvert.SerializeObject(result));

            _mediator.Verify(x => x.Send(It.IsAny<GetAboutInfo.Query>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
