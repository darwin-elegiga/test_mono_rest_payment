using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests.Batches
{
    public class GetBatchSummarySqlFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public GetBatchSummarySqlFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<GetBatchSummarySql.Handler>()
                    .AddMock<ILogger<GetBatchSummarySql.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var expectedResult = _container.Fake<BatchSummary.BatchSummary>(_ => _.With(_ => _.Status, BatchStatus.BatchInitiated));
            var query = _container.Fake<GetBatchSummarySql.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.GetFirstOrDefaultAsync<BatchSummary.BatchSummary>(Arg.Is<string>(_ => _.Equals("dbo.usp_InboundBatch_Summary", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetBatchSummarySql.Query>(_ => _ == query), Arg.Any<CancellationToken>())
                .Returns(expectedResult);

            var handler = _container.Get<GetBatchSummarySql.Handler>();
            Func<Task<BatchSummary.BatchSummary>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().GetFirstOrDefaultAsync<BatchSummary.BatchSummary>(Arg.Is<string>(_ => _.Equals("dbo.usp_InboundBatch_Summary", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetBatchSummarySql.Query>(_ => _ == query), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfGetFirstOrDefaultFailsWithException()
        {
            var query = _container.Fake<GetBatchSummarySql.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.When(_ => _.GetFirstOrDefaultAsync<BatchSummary.BatchSummary>(Arg.Is<string>(_ => _.Equals("dbo.usp_InboundBatch_Summary", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetBatchSummarySql.Query>(_ => _ == query), Arg.Any<CancellationToken>()))
                .Do(_ => throw new Exception());

            var handler = _container.Get<GetBatchSummarySql.Handler>();
            Func<Task<BatchSummary.BatchSummary>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().GetFirstOrDefaultAsync<BatchSummary.BatchSummary>(Arg.Is<string>(_ => _.Equals("dbo.usp_InboundBatch_Summary", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetBatchSummarySql.Query>(_ => _ == query), Arg.Any<CancellationToken>());
        }
    }
}
