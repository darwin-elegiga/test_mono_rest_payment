using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Api.Controllers;
using VPay.DocSys.Parsing.Common.Interfaces;
using Xunit;

namespace VPay.DocSys.Parsing.Api.Tests
{
    public class SupportControllerTests
    {
        private readonly Mock<IStatusFileTracker> _statusFileTrackerMock = new Mock<IStatusFileTracker>();
        private readonly Mock<IDriverFileService> _driverFileServiceMock = new Mock<IDriverFileService>();
        private readonly Mock<IStatusFileService> _statusFileServiceMock = new Mock<IStatusFileService>();
        private readonly Mock<IRedCardRepository> _redcardRepositoryMock = new Mock<IRedCardRepository>();
        private readonly Mock<IArchiveRecordService> _archiveRecordServiceMock = new Mock<IArchiveRecordService>();
        private readonly SupportController _sut;

        public SupportControllerTests()
        {
            _sut = new SupportController(
                _statusFileTrackerMock.Object,
                _driverFileServiceMock.Object,
                _statusFileServiceMock.Object,
                _redcardRepositoryMock.Object,
                _archiveRecordServiceMock.Object
                );
        }

        [Fact]
        public void Constructor_DefaultServiceCollection_ShouldReturnInstance()
        {
            // arrange
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile("appsettings.Development.json", true)
                .AddJsonFile("appsettings.LocalDevelopment.json", true).Build();
            var collection = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
            VPay.DocSys.Parsing.DependencyInjectionHelper
                .ConfigureDependencies(collection, configuration)
                .AddTransient<SupportController>();

            // actual
            var provider = collection.BuildServiceProvider();
            var controller = provider.GetRequiredService<SupportController>();

            // assert
            Assert.NotNull(controller);
        }

        [Theory]
        [MemberData(nameof(GivenWhenGetRedCardTransactionStatus))]
        public async Task WhenGetRedCardTransactionStatus(long givenTransactionId, IList<RedCardTransactionStatusCode> givenStatuses,
            long whenTransactionId, RedCardTransactionStatusCode expectedStatusCode)
        {
            // arrange
            GivenRedCardTransactionStatus(givenTransactionId, givenStatuses);

            // actual
            var actual = await _sut.GetRedCardTransactionStatus(whenTransactionId, default);

            // assert
            actual.Should().NotBeNull();
            actual.Should().BeOfType<OkObjectResult>();
            var result = actual as ObjectResult;
            result.Should().NotBeNull();
            result.StatusCode.Should().Be((int)HttpStatusCode.OK);
            var response = ReDeserialize(result.Value, new { CurrentStatus = RedCardTransactionStatusCode.NotFound, CurrentStatusCode = 0 });
            response.CurrentStatus.Should().Be(expectedStatusCode);
            response.CurrentStatusCode.Should().Be((int)expectedStatusCode);
        }

        [Theory]
        [MemberData(nameof(GivenWhenPutRedCardTransactionStatus))]
        public async Task WhenPutRedCardTransactionStatus(long givenTransactionId, IList<RedCardTransactionStatusCode> givenStatuses,
            long whenTransactionId, RedCardTransactionStatusCode whenStatusCode, int expectedRows, int expectedHttpStatusCode, string expectedExceptionMessage)
        {
            // arrange
            GivenRedCardTransactionStatus(givenTransactionId, givenStatuses);

            // actual
            Func<Task<IActionResult>> act = async () => await _sut.PutRedCardTransactionStatus(whenTransactionId, whenStatusCode, default);

            // assert
            if (!string.IsNullOrEmpty(expectedExceptionMessage))
            {
                (await act.Should().ThrowAsync<Exception>()).WithMessage(expectedExceptionMessage);
            }
            else
            {
                var actual = await act() as ObjectResult;
                actual.Should().NotBeNull();
                actual.StatusCode.Should().Be(expectedHttpStatusCode);
                var response = ReDeserialize(actual.Value, new { RowsInserted = -1 });
                response.RowsInserted.Should().Be(expectedRows);
            }
        }

        [Theory]
        [MemberData(nameof(GivenWhenDeleteRedCardTransactionStatus))]
        public async Task WhenDeleteRedCardTransactionStatus(long givenTransactionId, IList<RedCardTransactionStatusCode> givenStatuses,
            long whenTransactionId, RedCardTransactionStatusCode whenStatusCode, int expectedRows, int expectedHttpStatusCode)
        {
            // arrange
            GivenRedCardTransactionStatus(givenTransactionId, givenStatuses);

            // actual
            var actual = await _sut.DeleteRedCardTransactionStatus(whenTransactionId, whenStatusCode, default) as ObjectResult;

            // assert
            actual.Should().NotBeNull();
            actual.StatusCode.Should().Be(expectedHttpStatusCode);
            var response = ReDeserialize(actual.Value, new { RowsDeleted = -1 });
            response.RowsDeleted.Should().Be(expectedRows);
        }

        public static IEnumerable<object[]> GivenWhenGetRedCardTransactionStatus => new List<object[]>
        {
            new object[] { 111L, new List<RedCardTransactionStatusCode> { (RedCardTransactionStatusCode)10, (RedCardTransactionStatusCode)20 }, 111L, (RedCardTransactionStatusCode)20 },
            new object[] { 111L, new List<RedCardTransactionStatusCode> { (RedCardTransactionStatusCode)10, (RedCardTransactionStatusCode)20 }, 112L, (RedCardTransactionStatusCode)0 },
        };
        public static IEnumerable<object[]> GivenWhenPutRedCardTransactionStatus => new List<object[]>
        {
            new object[] { 111L, new List<RedCardTransactionStatusCode> { (RedCardTransactionStatusCode)10, (RedCardTransactionStatusCode)20 }, 110L, (RedCardTransactionStatusCode)10, 0, 0, SqlException_FK_RedCardTransactionStatus_DocumentTransaction },
            new object[] { 111L, new List<RedCardTransactionStatusCode> { (RedCardTransactionStatusCode)10, (RedCardTransactionStatusCode)20 }, 111L, (RedCardTransactionStatusCode)0, 0, (int)HttpStatusCode.BadRequest, null },
            new object[] { 111L, new List<RedCardTransactionStatusCode> { (RedCardTransactionStatusCode)10, (RedCardTransactionStatusCode)20 }, 111L, (RedCardTransactionStatusCode)1, 0, (int)HttpStatusCode.BadRequest, null },
            new object[] { 111L, new List<RedCardTransactionStatusCode> { (RedCardTransactionStatusCode)10, (RedCardTransactionStatusCode)20 }, 111L, (RedCardTransactionStatusCode)10, 1, (int)HttpStatusCode.OK, null },
            new object[] { 111L, new List<RedCardTransactionStatusCode> { (RedCardTransactionStatusCode)10, (RedCardTransactionStatusCode)20 }, 111L, (RedCardTransactionStatusCode)30, 1, (int)HttpStatusCode.OK, null },
        };
        public static IEnumerable<object[]> GivenWhenDeleteRedCardTransactionStatus => new List<object[]>
        {
            new object[] { 111L, new List<RedCardTransactionStatusCode> { (RedCardTransactionStatusCode)10, (RedCardTransactionStatusCode)20 }, 110L, (RedCardTransactionStatusCode)20, 0, (int)HttpStatusCode.OK },
            new object[] { 111L, new List<RedCardTransactionStatusCode> { (RedCardTransactionStatusCode)10, (RedCardTransactionStatusCode)20 }, 111L, (RedCardTransactionStatusCode)30, 0, (int)HttpStatusCode.OK },
            new object[] { 111L, new List<RedCardTransactionStatusCode> { (RedCardTransactionStatusCode)10, (RedCardTransactionStatusCode)20 }, 111L, (RedCardTransactionStatusCode)20, 1, (int)HttpStatusCode.OK },
            new object[] { 111L, new List<RedCardTransactionStatusCode> { (RedCardTransactionStatusCode)10, (RedCardTransactionStatusCode)20 }, 111L, (RedCardTransactionStatusCode)10, 0, (int)HttpStatusCode.BadRequest }
        };
        private const string SqlException_FK_RedCardTransactionStatus_DocumentTransaction = "The INSERT statement conflicted with the FOREIGN KEY constraint \"FK_RedCardTransactionStatus_DocumentTransaction\". The conflict occurred in database \"DocumentSystem\", table \"dbo.DocumentTransaction\", column 'TransactionId'.\nThe statement has been terminated.";
        private void GivenRedCardTransactionStatus(long transactionId, IList<RedCardTransactionStatusCode> statusCodes)
        {
            _redcardRepositoryMock
                .Setup(m => m.GetTransactionStatus(It.Is<long>(t => t == transactionId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(statusCodes.Max());
            _redcardRepositoryMock
                .Setup(m => m.GetTransactionStatus(It.Is<long>(t => t != transactionId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(RedCardTransactionStatusCode.NotFound);
            _redcardRepositoryMock
                .Setup(m => m.UpdateTransactionStatus(It.Is<long>(t => t == transactionId), It.IsAny<RedCardTransactionStatusCode>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _redcardRepositoryMock
                .Setup(m => m.UpdateTransactionStatus(It.Is<long>(t => t != transactionId), It.IsAny<RedCardTransactionStatusCode>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new System.Exception(SqlException_FK_RedCardTransactionStatus_DocumentTransaction));
            _redcardRepositoryMock
                .Setup(m => m.ClearTransactionStatus(It.Is<long>(t => t == transactionId), It.Is<RedCardTransactionStatusCode>(s => statusCodes.Contains(s)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _redcardRepositoryMock
                .Setup(m => m.ClearTransactionStatus(It.Is<long>(t => t == transactionId), It.Is<RedCardTransactionStatusCode>(s => !statusCodes.Contains(s)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
            _redcardRepositoryMock
                .Setup(m => m.ClearTransactionStatus(It.Is<long>(t => t != transactionId), It.IsAny<RedCardTransactionStatusCode>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
        }
        private T ReDeserialize<T>(object item, T anonymousType)
        {
            return JsonConvert.DeserializeAnonymousType<T>(JsonConvert.SerializeObject(item), anonymousType);
        }
    }
}
