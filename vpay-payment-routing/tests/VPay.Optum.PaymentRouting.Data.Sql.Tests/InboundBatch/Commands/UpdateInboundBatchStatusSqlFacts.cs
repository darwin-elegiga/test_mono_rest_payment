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
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests
{
    public class UpdateInboundBatchStatusSqlFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public UpdateInboundBatchStatusSqlFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<UpdateInboundBatchStatusSql.Handler>()
                    .AddMock<ILogger<UpdateInboundBatchStatusSql.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var command = new UpdateInboundBatchStatusSql.Command(100, BatchStatus.BatchReceived);
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.SaveExecuteAsync(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundBatchStatus_Update]", StringComparison.CurrentCultureIgnoreCase)),
                Arg.Is<UpdateInboundBatchStatusSql.Command>(_ => _ == command), Arg.Any<CancellationToken>())
                .Returns(1);

            var handler = _container.Get<UpdateInboundBatchStatusSql.Handler>();
            Func<Task<Unit>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().SaveExecuteAsync(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundBatchStatus_Update]", StringComparison.CurrentCultureIgnoreCase)),
               Arg.Is<UpdateInboundBatchStatusSql.Command>(_ => _ == command), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfSaveFailsWithException()
        {
            var command = new UpdateInboundBatchStatusSql.Command(100, BatchStatus.BatchReceived);
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.When(_ => _.SaveExecuteAsync(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundBatchStatus_Update]", StringComparison.CurrentCultureIgnoreCase)),
                Arg.Is<UpdateInboundBatchStatusSql.Command>(_ => _ == command), Arg.Any<CancellationToken>()))
                 .Do(_ => throw new Exception());

            var handler = _container.Get<UpdateInboundBatchStatusSql.Handler>();
            Func<Task<Unit>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().SaveExecuteAsync(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundBatchStatus_Update]", StringComparison.CurrentCultureIgnoreCase)),
               Arg.Is<UpdateInboundBatchStatusSql.Command>(_ => _ == command), Arg.Any<CancellationToken>());
        }
    }
}
