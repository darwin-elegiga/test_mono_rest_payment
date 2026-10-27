using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Queries;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests
{
    public class GetOutbound835FileByIdFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public GetOutbound835FileByIdFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<GetOutbound835FileById.Handler>()
                    .AddMock<ILogger<GetOutbound835FileById.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var expectedFile = _container.Fake<Models.Outbound835File>(_ => _.With(_ => _.Status, Outbound835FileStatus.Queued));
            var expectedPayments = _container.FakeMany<long>(10);
            var query = new GetOutbound835FileById.Query(1);
            var connection = _container.Get<IDataConnection<SqlConnection>>();
            var multipleMock = _container.Mock<IGridAccessor>();
            multipleMock.ReadFirstOrDefaultAsync<Models.Outbound835File>()
                .Returns(expectedFile);
            multipleMock.ReadAsync<long>()
                .Returns(expectedPayments);

            connection.GetMultipleAsync(Arg.Is<string>(_ => _.Equals("dbo.[usp_Outbound835File_GetById]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetOutbound835FileById.Query>(_ => _ == query), Arg.Any<CancellationToken>())
                .Returns(multipleMock);

            var handler = _container.Get<GetOutbound835FileById.Handler>();
            Func<Task<Models.Outbound835File>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().GetMultipleAsync(Arg.Is<string>(_ => _.Equals("dbo.[usp_Outbound835File_GetById]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetOutbound835FileById.Query>(_ => _ == query), Arg.Any<CancellationToken>());

            await multipleMock.Received().ReadFirstOrDefaultAsync<Models.Outbound835File>();
            await multipleMock.Received().ReadAsync<long>();
        }

        [Fact]
        public async Task Should_ThrowError_When_GetMultipleAsyncThrowsError()
        {
            var expectedFile = _container.Fake<Models.Outbound835File>(_ => _.With(_ => _.Status, Outbound835FileStatus.Queued));
            var expectedPayments = _container.FakeMany<long>(10);
            var query = new GetOutbound835FileById.Query(1);
            var connection = _container.Get<IDataConnection<SqlConnection>>();
            var multipleMock = _container.Mock<IGridAccessor>();
            multipleMock.ReadFirstOrDefaultAsync<Models.Outbound835File>()
                .Returns(expectedFile);
            multipleMock.ReadAsync<long>()
                .Returns(expectedPayments);

            connection.When(_ => _.GetMultipleAsync(Arg.Is<string>(_ => _.Equals("dbo.[usp_Outbound835File_GetById]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetOutbound835FileById.Query>(_ => _ == query), Arg.Any<CancellationToken>()))
                .Do(_ => throw new Exception());

            var handler = _container.Get<GetOutbound835FileById.Handler>();
            Func<Task<Models.Outbound835File>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().GetMultipleAsync(Arg.Is<string>(_ => _.Equals("dbo.[usp_Outbound835File_GetById]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetOutbound835FileById.Query>(_ => _ == query), Arg.Any<CancellationToken>());

            await multipleMock.DidNotReceive().ReadFirstOrDefaultAsync<Models.Outbound835File>();
            await multipleMock.DidNotReceive().ReadAsync<long>();
        }

        [Fact]
        public async Task Should_ThrowError_When_ReadFirstOrDefaultAsyncThrowsError()
        {
            var expectedFile = _container.Fake<Models.Outbound835File>(_ => _.With(_ => _.Status, Outbound835FileStatus.Queued));
            var expectedPayments = _container.FakeMany<long>(10);
            var query = new GetOutbound835FileById.Query(1);
            var connection = _container.Get<IDataConnection<SqlConnection>>();
            var multipleMock = _container.Mock<IGridAccessor>();
            multipleMock.When(_ => _.ReadFirstOrDefaultAsync<Models.Outbound835File>())
               .Do(_ => throw new Exception());
            multipleMock.ReadAsync<long>()
                .Returns(expectedPayments);

            connection.GetMultipleAsync(Arg.Is<string>(_ => _.Equals("dbo.[usp_Outbound835File_GetById]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetOutbound835FileById.Query>(_ => _ == query), Arg.Any<CancellationToken>())
                .Returns(multipleMock);

            var handler = _container.Get<GetOutbound835FileById.Handler>();
            Func<Task<Models.Outbound835File>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().GetMultipleAsync(Arg.Is<string>(_ => _.Equals("dbo.[usp_Outbound835File_GetById]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetOutbound835FileById.Query>(_ => _ == query), Arg.Any<CancellationToken>());

            await multipleMock.Received().ReadFirstOrDefaultAsync<Models.Outbound835File>();
            await multipleMock.DidNotReceive().ReadAsync<long>();
        }

        [Fact]
        public async Task Should_ThrowError_When_ReadAsyncThrowsError()
        {
            var expectedFile = _container.Fake<Models.Outbound835File>(_ => _.With(_ => _.Status, Outbound835FileStatus.Queued));
            var expectedPayments = _container.FakeMany<long>(10);
            var query = new GetOutbound835FileById.Query(1);
            var connection = _container.Get<IDataConnection<SqlConnection>>();
            var multipleMock = _container.Mock<IGridAccessor>();
            multipleMock.ReadFirstOrDefaultAsync<Models.Outbound835File>()
                .Returns(expectedFile);
            multipleMock.When(_ => _.ReadAsync<long>())
                .Do(_ => throw new Exception());

            connection.GetMultipleAsync(Arg.Is<string>(_ => _.Equals("dbo.[usp_Outbound835File_GetById]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetOutbound835FileById.Query>(_ => _ == query), Arg.Any<CancellationToken>())
                .Returns(multipleMock);

            var handler = _container.Get<GetOutbound835FileById.Handler>();
            Func<Task<Models.Outbound835File>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().GetMultipleAsync(Arg.Is<string>(_ => _.Equals("dbo.[usp_Outbound835File_GetById]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetOutbound835FileById.Query>(_ => _ == query), Arg.Any<CancellationToken>());

            await multipleMock.Received().ReadFirstOrDefaultAsync<Models.Outbound835File>();
            await multipleMock.Received().ReadAsync<long>();
        }
    }
}
