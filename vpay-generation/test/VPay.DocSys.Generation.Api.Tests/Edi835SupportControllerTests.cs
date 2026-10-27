using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Api.Controllers;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Edi835;
using Xunit;

namespace VPay.DocSys.Generation.Api.Tests
{
    public class Edi835SupportControllerTests
    {
        private readonly Mock<IEdi835CreationService> _edi835CreationServiceMock = new Mock<IEdi835CreationService>();
        private readonly Mock<IEdi835Repository> _edi835Repository = new Mock<IEdi835Repository>();
        private readonly Mock<IPrfTransactionRepository> _prfTransactionRepository = new Mock<IPrfTransactionRepository>();
        private readonly Mock<IDistributionRepository> _distributionRepository = new Mock<IDistributionRepository>();
        private readonly Mock<IEdi835Service> _edi835Service = new Mock<IEdi835Service>();
        private readonly ILogger<Edi835SupportController> _logger = new NullLogger<Edi835SupportController>();

        private readonly Mock<IServiceProvider> _serviceProvider = new Mock<IServiceProvider>();
        private readonly Mock<IServiceScope> _serviceScope = new Mock<IServiceScope>();
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory = new Mock<IServiceScopeFactory>();

        private Edi835SupportController _sut;
        public Edi835SupportControllerTests()
        {
            // These four lines are needed to setup the cache to return valid responses
            _edi835Repository.Setup(x => x.GetEdi835Configurations(CancellationToken.None)).ReturnsAsync(Edi835ConfigData());
            _serviceProvider.Setup(x => x.GetService(typeof(IEdi835Repository))).Returns(_edi835Repository.Object);
            _serviceScope.Setup(x => x.ServiceProvider).Returns(_serviceProvider.Object);
            _serviceScopeFactory.Setup(x => x.CreateScope()).Returns(_serviceScope.Object);            
        }

        #region PostPublishTransactions Tests

        [Fact]
        public async ValueTask PostPublishTransaction_Should_Return413RequestEntityTooLarge_WhenRequestIsTooManyTransactions()
        {
            // Arrange
            _sut = new Edi835SupportController(
                _logger,
                _edi835CreationServiceMock.Object,
                _edi835Repository.Object,
                _prfTransactionRepository.Object,
                _distributionRepository.Object,
                _edi835Service.Object,
                new Edi835ConfigurationCache(_serviceScopeFactory.Object)
                );
            var expectedMessage = "Due to timeout considerations, no more than 100 transactions can be submitted at one time. Please split your query into lists of 100 or less.";
            var request = new TransactionIdListRequest
            {
                TransactionIds = CreateRange(1, 101).ToList() // maxTransactions is currently 100
            };

            // Act
            var response = await _sut.PostPublishTransactions(request);

            // Assert
            response.Should()
                .Match<ObjectResult>(x =>
                    x.StatusCode == 413 &&
                    (string)x.Value == expectedMessage
                );
        }

        [Fact]
        public void PostPublishTransaction_Should_Throw_WhenServiceThrowsException()
        {
            // Arrange
            var expectedError = new Exception("This is a test Exception");
            _edi835Service
                .Setup(x => x.PublishTransactions(It.IsAny<IEnumerable<long>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(expectedError);

            _sut = new Edi835SupportController(
                _logger,
                _edi835CreationServiceMock.Object,
                _edi835Repository.Object,
                _prfTransactionRepository.Object,
                _distributionRepository.Object,
                _edi835Service.Object,
                new Edi835ConfigurationCache(_serviceScopeFactory.Object)
                );

            var request = new TransactionIdListRequest
            {
                TransactionIds = CreateRange(1, 10).ToList()
            };

            // Act
            Func<Task> action = async () => await _sut.PostPublishTransactions(request);

            // Assert
            action.Should().ThrowAsync<Exception>().Result.Which.Should().BeEquivalentTo(expectedError);
        }

        [Fact]
        public async ValueTask PostPublishTransaction_Should_Return202Accepted()
        {
            // Arrange
            var expected = new Dictionary<long, string>
            {
                { 1, "Message queued & status updated to QueuedForGeneration." },
                { 2, "Not an Edi835" },
                { 3, "Message queued & status updated to QueuedForGeneration." }
            };
            _edi835Service
                .Setup(x => x.PublishTransactions(It.IsAny<IEnumerable<long>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            _sut = new Edi835SupportController(
                _logger,
                _edi835CreationServiceMock.Object,
                _edi835Repository.Object,
                _prfTransactionRepository.Object,
                _distributionRepository.Object,
                _edi835Service.Object,
                new Edi835ConfigurationCache(_serviceScopeFactory.Object)
                );

            var request = new TransactionIdListRequest
            {
                TransactionIds = CreateRange(1, 3).ToList()
            };

            // Act
            var response = await _sut.PostPublishTransactions(request);

            // Assert
            response.Should()
                .Match<AcceptedResult>(x =>
                    x.StatusCode == 202 &&
                    x.Value == expected
                );
        }

        #endregion

        #region PokeSlowOpach Tests

        [Fact]
        public async ValueTask PokeSlowOpach_Should_Return202Accepted_Ideally()
        {
            // Arrange
            var expectedStuckOpachTxns = CreateRange(1, 4).ToList();
            var expectedPrfTxns = expectedStuckOpachTxns.Select(x => new PrfTransaction
            {
                Id = x,
                TransactionId = x,
                Is835Received = true
            }).ToList();
            var expectedResult = new Dictionary<long, string>
            {
                { 1, "Message queued & status updated to QueuedForGeneration." },
                { 2, "Not an Edi835" },
                { 3, "Message queued & status updated to QueuedForGeneration." },
                { 4, "Message queued & status updated to QueuedForGeneration." }
            };

            _edi835Repository
                .Setup(x => x.GetStuckOpachTransactions(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedStuckOpachTxns);
            _prfTransactionRepository
                .Setup(x => x.GetPrfTransactionsByTransactionIds(It.IsAny<IList<long>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedPrfTxns);
            _edi835Service
                .Setup(x => x.PublishTransactions(It.IsAny<IEnumerable<long>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            _sut = new Edi835SupportController(
                _logger,
                _edi835CreationServiceMock.Object,
                _edi835Repository.Object,
                _prfTransactionRepository.Object,
                _distributionRepository.Object,
                _edi835Service.Object,
                new Edi835ConfigurationCache(_serviceScopeFactory.Object)
                );

            // Act
            var response = await _sut.PokeSlowOpach();

            // Assert
            response.Should().BeOfType<AcceptedResult>()
                .Subject.StatusCode.Should().Be(202);

            (response as AcceptedResult).Value.Should().BeOfType<Dictionary<long, string>>()
                .And.BeEquivalentTo(expectedResult);
        }

        [Fact]
        public void PokeSlowOpach_Should_Throw_WhenServiceThrowsException()
        {
            // Arrange
            var expectedStuckOpachTxns = CreateRange(1, 4).ToList();
            var expectedPrfTxns = expectedStuckOpachTxns.Select(x => new PrfTransaction
            {
                Id = x,
                TransactionId = x,
                Is835Received = true
            }).ToList();
            var expectedResult = new Exception("This is a test Exception");

            _edi835Repository
                .Setup(x => x.GetStuckOpachTransactions(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedStuckOpachTxns);
            _prfTransactionRepository
                .Setup(x => x.GetPrfTransactionsByTransactionIds(It.IsAny<IList<long>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedPrfTxns);
            _edi835Service
                .Setup(x => x.PublishTransactions(It.IsAny<IEnumerable<long>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(expectedResult);

            _sut = new Edi835SupportController(
                _logger,
                _edi835CreationServiceMock.Object,
                _edi835Repository.Object,
                _prfTransactionRepository.Object,
                _distributionRepository.Object,
                _edi835Service.Object,
                new Edi835ConfigurationCache(_serviceScopeFactory.Object)
                );

            // Act
            Func<Task> action = async () => await _sut.PokeSlowOpach();

            // Assert
            action.Should().ThrowAsync<Exception>().Result.Which.Should().BeEquivalentTo(expectedResult);
        }

        #endregion

        [Fact]
        public void ClearCache_Should_Return200OK()
        {
            // Arrange
            _sut = new Edi835SupportController(
                _logger,
                _edi835CreationServiceMock.Object,
                _edi835Repository.Object,
                _prfTransactionRepository.Object,
                _distributionRepository.Object,
                _edi835Service.Object,
                new Edi835ConfigurationCache(_serviceScopeFactory.Object)
                );

            // Act
            var response =_sut.ClearCache();

            // Assert
            response.Should().NotBeNull();
            response.Should().BeOfType<OkResult>();
            var okResponse = response as OkResult;
            okResponse.Should().NotBeNull();
            okResponse.StatusCode.Should().Be((int)HttpStatusCode.OK);

        }

        public IEnumerable<long> CreateRange(long start, long count)
        {
            var limit = start + count;

            while (start < limit)
            {
                yield return start;
                start++;
            }
        }

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
