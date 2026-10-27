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
    public class InsertInboundPaymentSqlFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public InsertInboundPaymentSqlFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<InsertInboundPaymentSql.Handler>()
                    .AddMock<ILogger<InsertInboundPaymentSql.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var expectedResult = _container.Fake<BatchSummary.BatchSummary>(_ => _.With(_ => _.Status, BatchStatus.BatchInitiated));
            var command = _container.Fake<InsertInboundPaymentSql.Command>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.SaveExecuteScalarAsync<long>(Arg.Is<string>(_ => _.Equals("dbo.usp_InboundPayment_Insert", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<InsertInboundPaymentSql.Command>(_ => _ == command), Arg.Any<CancellationToken>())
                .Returns(1);

            connection.GetFirstOrDefaultAsync<BatchSummary.BatchSummary>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundBatch_Summary]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Any<object>(), Arg.Any<CancellationToken>())
                .Returns(expectedResult);

            var handler = _container.Get<InsertInboundPaymentSql.Handler>();
            Func<Task<BatchSummary.BatchSummary>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().SaveExecuteScalarAsync<long>(Arg.Is<string>(_ => _.Equals("dbo.usp_InboundPayment_Insert", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<InsertInboundPaymentSql.Command>(_ => _ == command), Arg.Any<CancellationToken>());

            await connection.Received().GetFirstOrDefaultAsync<BatchSummary.BatchSummary>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundBatch_Summary]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Any<object>(), Arg.Any<CancellationToken>());
        }
    }
}
