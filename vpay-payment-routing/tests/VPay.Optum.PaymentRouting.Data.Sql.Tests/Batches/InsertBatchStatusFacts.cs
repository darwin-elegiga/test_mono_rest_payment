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
    public class InsertBatchStatusFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public InsertBatchStatusFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<InsertBatchStatus.Handler>()
                    .AddMock<ILogger<InsertBatchStatus.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var command = _container.Fake<InsertBatchStatus.Command>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.SaveFirstOrDefaultAsync<int>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundBatchStatus_Update]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<InsertBatchStatus.Command>(_ => _ == command), Arg.Any<CancellationToken>())
                .Returns(0);

            var handler = _container.Get<InsertBatchStatus.Handler>();
            Func<Task<int>> act = () => handler.Handle(command, CancellationToken.None);

            await act.Should().NotThrowAsync();
            await connection.Received().SaveFirstOrDefaultAsync<int>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundBatchStatus_Update]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<InsertBatchStatus.Command>(_ => _ == command), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfGetFirstOrDefaultFailsWithException()
        {
            var command = _container.Fake<InsertBatchStatus.Command>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.When(_ => _.SaveFirstOrDefaultAsync<int>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundBatchStatus_Update]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<InsertBatchStatus.Command>(_ => _ == command), Arg.Any<CancellationToken>()))
                .Do(_ => throw new Exception());

            var handler = _container.Get<InsertBatchStatus.Handler>();

            Func<Task<int>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().SaveFirstOrDefaultAsync<int>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundBatchStatus_Update]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<InsertBatchStatus.Command>(_ => _ == command), Arg.Any<CancellationToken>());
        }
    }
}
