using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.InboundBatch.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.InboundBatch.Queries;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests.ApReap.Commands
{
    public class GetInboundBatchByOptumBatchIdSqlFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public GetInboundBatchByOptumBatchIdSqlFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<GetInboundBatchByOptumBatchIdSql.Handler>()
                    .AddMock<ILogger<GetInboundBatchByOptumBatchIdSql.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var expectedResult = _container.Fake<GetInboundBatchByOptumBatchIdSql.Result>(_ => _.With(_ => _.Status, BatchStatus.BatchComplete));
            var command = _container.Fake<GetInboundBatchByOptumBatchIdSql.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.GetFirstOrDefaultAsync<GetInboundBatchByOptumBatchIdSql.Result>(Arg.Is<string>(_ => _.Equals("dbo.usp_InboundBatch_GetByOptumBatchId", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetInboundBatchByOptumBatchIdSql.Query>(_ => _ == command), Arg.Any<CancellationToken>())
                .Returns(expectedResult);

            var handler = _container.Get<GetInboundBatchByOptumBatchIdSql.Handler>();
            Func<Task<GetInboundBatchByOptumBatchIdSql.Result>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().GetFirstOrDefaultAsync<GetInboundBatchByOptumBatchIdSql.Result>(Arg.Is<string>(_ => _.Equals("dbo.usp_InboundBatch_GetByOptumBatchId", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetInboundBatchByOptumBatchIdSql.Query>(_ => _ == command), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfSavesFailsWithException()
        {
            var command = _container.Fake<GetInboundBatchByOptumBatchIdSql.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.When(_ => _.GetFirstOrDefaultAsync<GetInboundBatchByOptumBatchIdSql.Result>(Arg.Is<string>(_ => _.Equals("dbo.usp_InboundBatch_GetByOptumBatchId", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetInboundBatchByOptumBatchIdSql.Query>(_ => _ == command), Arg.Any<CancellationToken>()))
                .Do(_ => throw new Exception());

            var handler = _container.Get<GetInboundBatchByOptumBatchIdSql.Handler>();
            Func<Task<GetInboundBatchByOptumBatchIdSql.Result>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().GetFirstOrDefaultAsync<GetInboundBatchByOptumBatchIdSql.Result>(Arg.Is<string>(_ => _.Equals("dbo.usp_InboundBatch_GetByOptumBatchId", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetInboundBatchByOptumBatchIdSql.Query>(_ => _ == command), Arg.Any<CancellationToken>());
        }
    }
}
