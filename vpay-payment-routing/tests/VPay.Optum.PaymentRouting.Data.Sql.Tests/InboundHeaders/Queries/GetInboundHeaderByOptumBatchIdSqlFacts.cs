using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.InboundHeader.Queries;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests.InboundHeaders.Queries
{
    public class GetInboundHeaderByOptumBatchIdSqlFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public GetInboundHeaderByOptumBatchIdSqlFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<GetInboundHeaderByOptumBatchIdSql.Handler>()
                    .AddMock<ILogger<GetInboundHeaderByOptumBatchIdSql.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var expectedResult = _container.Fake<GetInboundHeaderByOptumBatchIdSql.Result>();
            var command = _container.Fake<GetInboundHeaderByOptumBatchIdSql.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.GetFirstOrDefaultAsync<GetInboundHeaderByOptumBatchIdSql.Result>(Arg.Is<string>(_ => _.Equals("[dbo].[usp_InboundHeader_GetByOptumBatchId]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetInboundHeaderByOptumBatchIdSql.Query>(_ => _ == command), Arg.Any<CancellationToken>())
                .Returns(expectedResult);

            var handler = _container.Get<GetInboundHeaderByOptumBatchIdSql.Handler>();
            Func<Task<GetInboundHeaderByOptumBatchIdSql.Result>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().GetFirstOrDefaultAsync<GetInboundHeaderByOptumBatchIdSql.Result>(Arg.Is<string>(_ => _.Equals("[dbo].[usp_InboundHeader_GetByOptumBatchId]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetInboundHeaderByOptumBatchIdSql.Query>(_ => _ == command), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfSavesFailsWithException()
        {
            var command = _container.Fake<GetInboundHeaderByOptumBatchIdSql.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.When(_ => _.GetFirstOrDefaultAsync<GetInboundHeaderByOptumBatchIdSql.Result>(Arg.Is<string>(_ => _.Equals("[dbo].[usp_InboundHeader_GetByOptumBatchId]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetInboundHeaderByOptumBatchIdSql.Query>(_ => _ == command), Arg.Any<CancellationToken>()))
                .Do(_ => throw new Exception());

            var handler = _container.Get<GetInboundHeaderByOptumBatchIdSql.Handler>();
            Func<Task<GetInboundHeaderByOptumBatchIdSql.Result>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().GetFirstOrDefaultAsync<GetInboundHeaderByOptumBatchIdSql.Result>(Arg.Is<string>(_ => _.Equals("[dbo].[usp_InboundHeader_GetByOptumBatchId]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetInboundHeaderByOptumBatchIdSql.Query>(_ => _ == command), Arg.Any<CancellationToken>());
        }
    }
}
