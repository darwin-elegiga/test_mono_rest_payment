using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Api.Controllers;
using VPay.DocSys.Parsing.Common;
using Xunit;

namespace VPay.DocSys.Parsing.Api.Tests
{
    public class Edi835SupportControllerTests
    {
        private Mock<IStatusFilePathHelper> _statusFilePathHelper = new Mock<IStatusFilePathHelper>();
        private Mock<IEdi835TarService> _edi835TarService = new Mock<IEdi835TarService>();
        private Mock<IEdi835Repository> _edi835Repository = new Mock<IEdi835Repository>();
        private Mock<IInbound835FileService> _inbound835FileService = new Mock<IInbound835FileService>();
        private Mock<ILoggerFactory> _loggerFactory = new Mock<ILoggerFactory>();
        private readonly Edi835GenerationQueueSettings _edi835GenerationQueueSettings = new Edi835GenerationQueueSettings();

        private readonly Mock<IServiceProvider> _serviceProvider = new Mock<IServiceProvider>();
        private readonly Mock<IServiceScope> _serviceScope = new Mock<IServiceScope>();
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory = new Mock<IServiceScopeFactory>();

        private readonly Edi835SupportController _sut;

        public Edi835SupportControllerTests()
        {
            // These four lines are needed to setup the cache to return valid responses
            _edi835Repository.Setup(x => x.GetEdi835Configurations(CancellationToken.None)).ReturnsAsync(Edi835ConfigData());
            _serviceProvider.Setup(x => x.GetService(typeof(IEdi835Repository))).Returns(_edi835Repository.Object);
            _serviceScope.Setup(x => x.ServiceProvider).Returns(_serviceProvider.Object);
            _serviceScopeFactory.Setup(x => x.CreateScope()).Returns(_serviceScope.Object);

            _sut = new Edi835SupportController(
                _statusFilePathHelper.Object,
                _edi835TarService.Object,
                _edi835Repository.Object,
                _edi835GenerationQueueSettings,
                new Edi835ConfigurationCache(_serviceScopeFactory.Object),
                _inbound835FileService.Object,
                _loggerFactory.Object);


        }

        [Fact]
        public void PostSendBRNsToAvaility_Should_Return202Ok_WithMessage()
        {
            // Arrange
            long txnId = 12345;
            var request = new TransactionOperationRequest { TransactionIds = new List<long> { txnId } };
            var expected = new List<TransactionOperationResponse>
            {
                new TransactionOperationResponse { TransactionId = txnId, Result = "Success" }
            };
            _inbound835FileService
                .Setup(x => x.SendBrnToAvaility(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var response = _sut.PostSendBRNsToAvaility(request, CancellationToken.None).Result;

            // Assert
            response.Should().NotBeNull();
            response.Should().BeOfType<OkObjectResult>();
            var okResponse = response as OkObjectResult;
            okResponse.Should().NotBeNull();
            okResponse.StatusCode.Should().Be((int)HttpStatusCode.OK);
            okResponse.Value.Should().BeOfType(expected.GetType());
            (okResponse.Value as List<TransactionOperationResponse>).First().Result.Should().Be(expected.First().Result);
            (okResponse.Value as List<TransactionOperationResponse>).First().TransactionId.Should().Be(expected.First().TransactionId);
        }

        #region GetEdi835Configuration
        [Theory]
        [InlineData("SWK", "SWK")]
        [InlineData("HEQTN", "HEQTN")]
        [InlineData("EHI", "DirectSpec")]
        [InlineData("HPS", "DualSpec")]
        public void GetEdi835Configuration_Should_Return200OK_WithMatchedParser(string clientCode, string expected)
        {
            // Arrange
            _edi835Repository
                .Setup(x => x.GetEdi835Configurations(CancellationToken.None))
                .ReturnsAsync(Edi835ConfigData());

            // Act
            var response = _sut.GetEdi835Configuration(clientCode, CancellationToken.None).Result;

            // Assert
            response.Should().NotBeNull();
            response.Should().BeOfType<OkObjectResult>();
            var okResponse = response as OkObjectResult;
            okResponse.Should().NotBeNull();
            okResponse.StatusCode.Should().Be((int)HttpStatusCode.OK);
            okResponse.Value.Should().Be(expected);

        }

        [Fact]
        public void GetEdi835Configuration_Should_Return400BadRequest_WithMessage()
        {
            // Arrange
            var expected = "clientCode must be provided";

            // Act
            var response = _sut.GetEdi835Configuration(string.Empty, CancellationToken.None).Result;

            // Assert
            response.Should().NotBeNull();
            response.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResponse = response as BadRequestObjectResult;
            badRequestResponse.Should().NotBeNull();
            badRequestResponse.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            badRequestResponse.Value.Should().Be(expected);
        }

        [Fact]
        public void GetEdi835Configuration_Should_Return404NotFound()
        {
            // Arrange
            _edi835Repository
                .Setup(x => x.GetEdi835Configurations(CancellationToken.None))
                .ReturnsAsync(Edi835ConfigData());
            var clientCode = "NO";

            // Act
            var response = _sut.GetEdi835Configuration(clientCode, CancellationToken.None).Result;

            // Assert
            response.Should().NotBeNull();
            response.Should().BeOfType<NotFoundResult>();
            var notFoundResponse = response as NotFoundResult;
            notFoundResponse.Should().NotBeNull();
            notFoundResponse.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        [Fact]
        public void GetEdi835Configuration_Should_Return500InternalServerError_WithException()
        {
            // Arrange
            var clientCode = "Test";
            var expected = new Exception("Error retrieving Edi835Config data");
            _edi835Repository
                .Setup(x => x.GetEdi835Configurations(CancellationToken.None))
                .ThrowsAsync(expected);

            // Act
            var response = _sut.GetEdi835Configuration(clientCode, CancellationToken.None).Result;

            // Assert
            response.Should().NotBeNull();
            response.Should().BeOfType<ObjectResult>();
            var genericResponse = response as ObjectResult;
            genericResponse.Should().NotBeNull();
            genericResponse.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
            genericResponse.Value.Should().Be(expected.ToString());
        }
        #endregion

        #region InsertEdi835Configuration
        [Theory]
        [InlineData("SWK", Edi835Parser.Swk)]
        [InlineData("HEQTN", Edi835Parser.Heqtn)]
        [InlineData("EHI", Edi835Parser.DirectSpec)]
        [InlineData("HPS", Edi835Parser.DualSpec)]
        public void InsertEdi835Configuration_Should_Return200OK(string clientCode, string parser)
        {
            // Arrange
            _edi835Repository
                .Setup(x => x.InsertEdi835Configuration(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None))
                .Returns(Task.CompletedTask);

            // Act
            var response = _sut.InsertEdi835Configuration(clientCode, parser, CancellationToken.None).Result;

            // Assert
            response.Should().NotBeNull();
            response.Should().BeOfType<OkResult>();
            var okResponse = response as OkResult;
            okResponse.Should().NotBeNull();
            okResponse.StatusCode.Should().Be((int)HttpStatusCode.OK);
        }

        [Theory]
        [InlineData("SWK", "", "clientCode and parser must be provided")]
        [InlineData("", Edi835Parser.Heqtn, "clientCode and parser must be provided")]
        [InlineData("SWK", "InvalidParser", "Parser selection is invalid")]
        public void InsertEdi835Configuration_Should_Return400BadRequest_WithMessage(string clientCode, string parser, string expected)
        {
            // Act
            var response = _sut.InsertEdi835Configuration(clientCode, parser, CancellationToken.None).Result;

            // Assert
            response.Should().NotBeNull();
            response.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResponse = response as BadRequestObjectResult;
            badRequestResponse.Should().NotBeNull();
            badRequestResponse.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            badRequestResponse.Value.Should().Be(expected);
        }

        [Fact]
        public void InsertEdi835Configuration_Should_Return500InternalServerError_WithException()
        {
            // Arrange
            var clientCode = "Test";
            var parser = Edi835Parser.Swk;
            var expected = new Exception("Error retrieving Edi835Config data");

            _edi835Repository
                .Setup(x => x.InsertEdi835Configuration(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None))
                .ThrowsAsync(expected);

            // Act
            var response = _sut.InsertEdi835Configuration(clientCode, parser, CancellationToken.None).Result;

            // Assert
            response.Should().NotBeNull();
            response.Should().BeOfType<ObjectResult>();
            var genericResponse = response as ObjectResult;
            genericResponse.Should().NotBeNull();
            genericResponse.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
            genericResponse.Value.Should().Be(expected.ToString());
        }
        #endregion

        #region ClearCache

        [Fact]
        public void ClearCache_Should_Return200OK()
        {
            // Act
            var response = _sut.ClearCache();

            // Assert
            response.Should().NotBeNull();
            response.Should().BeOfType<OkResult>();
            var okResponse = response as OkResult;
            okResponse.Should().NotBeNull();
            okResponse.StatusCode.Should().Be((int)HttpStatusCode.OK);
        }

        #endregion

        private IList<Edi835Configuration> Edi835ConfigData()
        {
            return new List<Edi835Configuration>
            {
                new Edi835Configuration
                {
                    Edi835ConfigurationId = 1,
                    ClientCode = "SWK",
                    Parser = "SWK",
                },
                new Edi835Configuration
                {
                    Edi835ConfigurationId = 2,
                    ClientCode = "HEQTN",
                    Parser = "HEQTN",
                },
                new Edi835Configuration
                {
                    Edi835ConfigurationId = 3,
                    ClientCode = "RC",
                    Parser = "RC",
                },
                new Edi835Configuration
                {
                    Edi835ConfigurationId = 4,
                    ClientCode = "EHI",
                    Parser = "DirectSpec",
                },
                new Edi835Configuration
                {
                    Edi835ConfigurationId = 4,
                    ClientCode = "HPS",
                    Parser = "DualSpec",
                }
            };
        }
    }
}
