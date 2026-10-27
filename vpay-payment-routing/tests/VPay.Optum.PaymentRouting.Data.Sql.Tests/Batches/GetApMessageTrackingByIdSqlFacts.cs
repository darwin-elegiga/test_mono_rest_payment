using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.BatchManifest;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests.Batches
{
    public class GetBatchManifestSqlFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public GetBatchManifestSqlFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<GetBatchManifestSql.Handler>()
                    .AddMock<ILogger<GetBatchManifestSql.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var expectedManifest = _container.FakeMany<BatchManifest.BatchManifest>(_ => _.With(_ => _.Status, BatchStatus.BatchInitiated), 1);
            var inboundPaymentIds = _container.FakeMany<long>(10);
            var connection = _container.Get<IDataConnection<SqlConnection>>();
            var multipleMock = _container.Mock<IGridAccessor>();
            multipleMock.ReadAsync<BatchManifest.BatchManifest>()
                .Returns(expectedManifest);
            multipleMock.ReadAsync<long>()
                .Returns(inboundPaymentIds);

            var query = _container.Fake<GetBatchManifestSql.Query>();

            connection.GetMultipleAsync(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundBatch_GetManifest]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetBatchManifestSql.Query>(_ => _ == query), Arg.Any<CancellationToken>())
                .Returns(multipleMock);

            var handler = _container.Get<GetBatchManifestSql.Handler>();
            Func<Task<BatchManifest.BatchManifest>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().GetMultipleAsync(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundBatch_GetManifest]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetBatchManifestSql.Query>(_ => _ == query), Arg.Any<CancellationToken>());
            await multipleMock.Received().ReadAsync<BatchManifest.BatchManifest>();
            await multipleMock.Received().ReadAsync<long>();
        }

        [Fact]
        public async Task Should_ThrowErrorIfGetMultipleAsyncFailsWithException()
        {
            var expectedManifest = _container.FakeMany<BatchManifest.BatchManifest>(_ => _.With(_ => _.Status, BatchStatus.BatchInitiated), 1);
            var inboundPaymentIds = _container.FakeMany<long>(10);
            var connection = _container.Get<IDataConnection<SqlConnection>>();
            var multipleMock = _container.Mock<IGridAccessor>();
            multipleMock.ReadAsync<BatchManifest.BatchManifest>()
                .Returns(expectedManifest);
            multipleMock.ReadAsync<long>()
                .Returns(inboundPaymentIds);

            var query = _container.Fake<GetBatchManifestSql.Query>();

            connection.When(_ => _.GetMultipleAsync(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundBatch_GetManifest]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetBatchManifestSql.Query>(_ => _ == query), Arg.Any<CancellationToken>()))
                .Do(_ => throw new Exception());

            var handler = _container.Get<GetBatchManifestSql.Handler>();
            Func<Task<BatchManifest.BatchManifest>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().GetMultipleAsync(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundBatch_GetManifest]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetBatchManifestSql.Query>(_ => _ == query), Arg.Any<CancellationToken>());
            await multipleMock.DidNotReceive().ReadAsync<BatchManifest.BatchManifest>();
            await multipleMock.DidNotReceive().ReadAsync<long>();
        }
    }
}
