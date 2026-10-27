using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.Gateway.Api.Controllers;
using VPay.Gateway.Application;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Common.Preferencing.Models;
using Xunit;

namespace VPay.Gateway.Api.Tests.Controllers;

public class PreValidationControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly ILogger<PreValidationController> _logger;

    public PreValidationControllerTests()
    {
        _mockMediator = new Mock<IMediator>(MockBehavior.Strict);
        _logger = new NullLogger<PreValidationController>();
    }

    public class Constructor : PreValidationControllerTests
    {
#pragma warning disable CA1806
        [Fact]
        public void MediatorMissing_Throws_ArgumentNullException() =>
            new Action(() => new PreValidationController(null, _logger))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("mediator");

        [Fact]
        public void LoggerMissing_Throws_ArgumentNullException() =>
            new Action(() => new PreValidationController(_mockMediator.Object, null))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("logger");
#pragma warning restore CA1806
    }

    public class GetMonetizationStatusTests : PreValidationControllerTests
    {
        private const string ClientCode = "ABC";
        private const string CorrelationId = "12345";
        private const string BillingEntityCode = "ABC0001001";
        private const string OriginCode = "SEMED";
        private const string ProviderType = "WARRANTY";

        [Fact]
        public async Task WhenRequestIsValid_ReturnsMonetizationResult()
        {
            // Arrange
            var controller = new PreValidationController(_mockMediator.Object, _logger);
            var request = CreateValidRequest();

            var billingEntities = new List<BillingEntityDto>
            {
                new BillingEntityDto
                {
                    ClientCode = ClientCode,
                    BillingEntityCode = BillingEntityCode,
                    Type = new BillingEntityTypeDto { Name = ProviderType }
                }
            };

            var originClient = new OriginClientDto
            {
                ClientCode = ClientCode,
                Origin = new OriginInfoDto { OriginCode = OriginCode }
            };

            var monetizationResult = new MonetizationResult
            {
                CorrelationId = CorrelationId,
                ValidatedPayee = true
            };

            _mockMediator.Setup(x => x.Send(
                It.Is<BillingEntitiesQuery>(q => q.ClientCode == ClientCode),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(billingEntities)
                .Verifiable();

            _mockMediator.Setup(x => x.Send(
                It.Is<OriginClientQuery>(q => q.ClientCode == ClientCode),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(originClient)
                .Verifiable();

            _mockMediator.Setup(x => x.Send(
                It.Is<GetPreferencingMonetization.Query>(q =>
                    q.Request.ClientCode == ClientCode &&
                    q.Request.OriginCode == OriginCode &&
                    q.Request.ProviderType == ProviderType),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(monetizationResult)
                .Verifiable();

            // Act
            var result = await controller.GetMonetizationStatus(request, CancellationToken.None);

            result.Should().NotBeNull();
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(new VPay.Gateway.Api.Models.MonetizationResultDto
            {
                CorrelationId = CorrelationId,
                ValidatedPayee = true
            });
            _mockMediator.VerifyAll();
        }

        [Fact]
        public async Task WhenRequestIsInvalid_ReturnsBadRequestProblem()
        {
            // Arrange
            var controller = new PreValidationController(_mockMediator.Object, _logger);

            // Act
            var result = await controller.GetMonetizationStatus(null, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<ObjectResult>();
            var objectResult = result.Result as ObjectResult;
            objectResult.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task WhenBillingEntitiesQueryFails_ReturnsServerErrorProblem()
        {
            // Arrange
            var controller = new PreValidationController(_mockMediator.Object, _logger);
            var request = CreateValidRequest();

            _mockMediator.Setup(x => x.Send(
                It.Is<BillingEntitiesQuery>(q => q.ClientCode == ClientCode),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database error"))
                .Verifiable();

            // Act
            var result = await controller.GetMonetizationStatus(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<ObjectResult>();
            var problemResult = result.Result as ObjectResult;
            problemResult.StatusCode.Should().Be(500);

            _mockMediator.VerifyAll();
        }

        [Fact]
        public async Task WhenOriginClientQueryFails_ReturnsServerErrorProblem()
        {
            // Arrange
            var controller = new PreValidationController(_mockMediator.Object, _logger);
            var request = CreateValidRequest();

            var billingEntities = new List<BillingEntityDto>
            {
                new BillingEntityDto
                {
                    ClientCode = ClientCode,
                    BillingEntityCode = BillingEntityCode,
                    Type = new BillingEntityTypeDto { Name = ProviderType }
                }
            };

            _mockMediator.Setup(x => x.Send(
                It.Is<BillingEntitiesQuery>(q => q.ClientCode == ClientCode),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(billingEntities)
                .Verifiable();

            _mockMediator.Setup(x => x.Send(
                It.Is<OriginClientQuery>(q => q.ClientCode == ClientCode),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Origin not found"))
                .Verifiable();

            // Act
            var result = await controller.GetMonetizationStatus(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<ObjectResult>();
            var objectResult = result.Result as ObjectResult;
            objectResult.StatusCode.Should().Be(500);

            _mockMediator.VerifyAll();
        }
        [Fact]
        public async Task WhenOriginClientQueryReturnsNull_ReturnsNotFoundProblem()
        {
            // Arrange
            var controller = new PreValidationController(_mockMediator.Object, _logger);
            var request = CreateValidRequest();
            var billingEntities = new List<BillingEntityDto>
            {
                new BillingEntityDto
                {
                    ClientCode = ClientCode,
                    BillingEntityCode = BillingEntityCode,
                    Type = new BillingEntityTypeDto { Name = ProviderType }
                }
            };
            _mockMediator.Setup(x => x.Send(
                It.Is<BillingEntitiesQuery>(q => q.ClientCode == ClientCode),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(billingEntities)
                .Verifiable();
            _mockMediator.Setup(x => x.Send(
                It.Is<OriginClientQuery>(q => q.ClientCode == ClientCode),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((OriginClientDto)null)
                .Verifiable();
            // Act
            var result = await controller.GetMonetizationStatus(request, CancellationToken.None);
            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<ObjectResult>();
            var objectResult = result.Result as ObjectResult;
            objectResult.StatusCode.Should().Be(404);
            _mockMediator.VerifyAll();
        }

        [Fact]
        public async Task WhenMonetizationQueryFails_ReturnsServerErrorProblem()
        {
            // Arrange
            var controller = new PreValidationController(_mockMediator.Object, _logger);
            var request = CreateValidRequest();

            var billingEntities = new List<BillingEntityDto>
            {
                new BillingEntityDto
                {
                    ClientCode = ClientCode,
                    BillingEntityCode = BillingEntityCode,
                    Type = new BillingEntityTypeDto { Name = ProviderType }
                }
            };

            var originClient = new OriginClientDto
            {
                ClientCode = ClientCode,
                Origin = new OriginInfoDto { OriginCode = OriginCode }
            };

            _mockMediator.Setup(x => x.Send(
                It.Is<BillingEntitiesQuery>(q => q.ClientCode == ClientCode),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(billingEntities)
                .Verifiable();

            _mockMediator.Setup(x => x.Send(
                It.Is<OriginClientQuery>(q => q.ClientCode == ClientCode),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(originClient)
                .Verifiable();

            _mockMediator.Setup(x => x.Send(
                It.IsAny<GetPreferencingMonetization.Query>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Monetization service error"))
                .Verifiable();

            // Act
            var result = await controller.GetMonetizationStatus(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<ObjectResult>();
            var objectResult = result.Result as ObjectResult;
            objectResult.StatusCode.Should().Be(500);

            _mockMediator.VerifyAll();
        }

        [Fact]
        public async Task WhenBillingEntitiesIsEmpty_StillProcessesRequest()
        {
            // Arrange
            var controller = new PreValidationController(_mockMediator.Object, _logger);
            var request = CreateValidRequest();

            var billingEntities = new List<BillingEntityDto>();

            var originClient = new OriginClientDto
            {
                ClientCode = ClientCode,
                Origin = new OriginInfoDto { OriginCode = OriginCode }
            };

            var monetizationResult = new MonetizationResult
            {
                CorrelationId = CorrelationId,
                ValidatedPayee = false
            };

            _mockMediator.Setup(x => x.Send(
                It.Is<BillingEntitiesQuery>(q => q.ClientCode == ClientCode),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(billingEntities)
                .Verifiable();

            _mockMediator.Setup(x => x.Send(
                It.Is<OriginClientQuery>(q => q.ClientCode == ClientCode),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(originClient)
                .Verifiable();

            _mockMediator.Setup(x => x.Send(
                It.Is<GetPreferencingMonetization.Query>(q =>
                    q.Request.ClientCode == ClientCode &&
                    q.Request.OriginCode == OriginCode &&
                    q.Request.ProviderType == string.Empty),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(monetizationResult)
                .Verifiable();

            // Act
            var result = await controller.GetMonetizationStatus(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<OkObjectResult>();

            _mockMediator.VerifyAll();
        }

        [Fact]
        public async Task WhenPlus4HasWhitespace_TrimsIt()
        {
            // Arrange
            var controller = new PreValidationController(_mockMediator.Object, _logger);
            var request = CreateValidRequest();
            request.Plus4 = "  1234  ";

            var billingEntities = new List<BillingEntityDto>
            {
                new BillingEntityDto
                {
                    ClientCode = ClientCode,
                    BillingEntityCode = BillingEntityCode,
                    Type = new BillingEntityTypeDto { Name = ProviderType }
                }
            };

            var originClient = new OriginClientDto
            {
                ClientCode = ClientCode,
                Origin = new OriginInfoDto { OriginCode = OriginCode }
            };

            var monetizationResult = new MonetizationResult
            {
                CorrelationId = CorrelationId,
                ValidatedPayee = true
            };

            _mockMediator.Setup(x => x.Send(
                It.IsAny<BillingEntitiesQuery>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(billingEntities)
                .Verifiable();

            _mockMediator.Setup(x => x.Send(
                It.IsAny<OriginClientQuery>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(originClient)
                .Verifiable();

            _mockMediator.Setup(x => x.Send(
                It.Is<GetPreferencingMonetization.Query>(q => q.Request.Plus4 == "1234"),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(monetizationResult)
                .Verifiable();

            // Act
            var result = await controller.GetMonetizationStatus(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<OkObjectResult>();

            _mockMediator.VerifyAll();
        }

        [Fact]
        public async Task WhenRequestHasNullPlus4_HandlesGracefully()
        {
            // Arrange
            var controller = new PreValidationController(_mockMediator.Object, _logger);
            var request = CreateValidRequest();
            request.Plus4 = null;

            var billingEntities = new List<BillingEntityDto>
            {
                new BillingEntityDto
                {
                    ClientCode = ClientCode,
                    BillingEntityCode = BillingEntityCode,
                    Type = new BillingEntityTypeDto { Name = ProviderType }
                }
            };

            var originClient = new OriginClientDto
            {
                ClientCode = ClientCode,
                Origin = new OriginInfoDto { OriginCode = OriginCode }
            };

            var monetizationResult = new MonetizationResult
            {
                CorrelationId = CorrelationId,
                ValidatedPayee = true
            };

            _mockMediator.Setup(x => x.Send(
                It.IsAny<BillingEntitiesQuery>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(billingEntities)
                .Verifiable();

            _mockMediator.Setup(x => x.Send(
                It.IsAny<OriginClientQuery>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(originClient)
                .Verifiable();

            _mockMediator.Setup(x => x.Send(
                It.Is<GetPreferencingMonetization.Query>(q => q.Request.Plus4 == null),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(monetizationResult)
                .Verifiable();

            // Act
            var result = await controller.GetMonetizationStatus(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<OkObjectResult>();

            _mockMediator.VerifyAll();
        }

        [Fact]
        public async Task WhenAllFieldsAreProvided_MapsCorrectly()
        {
            // Arrange
            var controller = new PreValidationController(_mockMediator.Object, _logger);
            var request = new MonetizationLookupDto
            {
                ClientCode = ClientCode,
                CorrelationID = CorrelationId,
                Amount = 150.75m,
                BillingEntityCode = BillingEntityCode,
                City = "Minneapolis",
                ProviderAddress1 = "123 Main St",
                ProviderAddress2 = "Suite 100",
                ProviderName = "Test Provider",
                State = "MN",
                TaxId = "123456789",
                ZipCode = "55401",
                Plus4 = "1234"
            };

            var billingEntities = new List<BillingEntityDto>
            {
                new BillingEntityDto
                {
                    ClientCode = ClientCode,
                    BillingEntityCode = BillingEntityCode,
                    Type = new BillingEntityTypeDto { Name = ProviderType }
                }
            };

            var originClient = new OriginClientDto
            {
                ClientCode = ClientCode,
                Origin = new OriginInfoDto { OriginCode = OriginCode }
            };

            var monetizationResult = new MonetizationResult
            {
                CorrelationId = CorrelationId,
                ValidatedPayee = true
            };

            _mockMediator.Setup(x => x.Send(
                It.IsAny<BillingEntitiesQuery>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(billingEntities)
                .Verifiable();

            _mockMediator.Setup(x => x.Send(
                It.IsAny<OriginClientQuery>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(originClient)
                .Verifiable();

            _mockMediator.Setup(x => x.Send(
                It.Is<GetPreferencingMonetization.Query>(q =>
                    q.Request.ClientCode == ClientCode &&
                    q.Request.CorrelationID == CorrelationId &&
                    q.Request.Amount == 150.75m &&
                    q.Request.BillingEntityCode == BillingEntityCode &&
                    q.Request.City == "Minneapolis" &&
                    q.Request.ProviderAddress1 == "123 Main St" &&
                    q.Request.ProviderAddress2 == "Suite 100" &&
                    q.Request.ProviderName == "Test Provider" &&
                    q.Request.State == "MN" &&
                    q.Request.TaxId == "123456789" &&
                    q.Request.ZipCode == "55401" &&
                    q.Request.Plus4 == "1234" &&
                    q.Request.OriginCode == OriginCode &&
                    q.Request.ProviderType == ProviderType),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(monetizationResult)
                .Verifiable();

            // Act
            var result = await controller.GetMonetizationStatus(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<OkObjectResult>();

            _mockMediator.VerifyAll();
        }

        private static MonetizationLookupDto CreateValidRequest()
        {
            return new MonetizationLookupDto
            {
                ClientCode = ClientCode,
                CorrelationID = CorrelationId,
                Amount = 100.00m,
                BillingEntityCode = BillingEntityCode,
                City = "Test City",
                ProviderAddress1 = "123 Test St",
                ProviderName = "Test Provider",
                State = "MN",
                TaxId = "123456789",
                ZipCode = "55401"
            };
        }
    }
}
