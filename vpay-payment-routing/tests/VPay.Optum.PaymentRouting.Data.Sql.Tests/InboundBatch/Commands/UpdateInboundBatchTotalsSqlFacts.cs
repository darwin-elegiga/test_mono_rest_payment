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
    public class UpdateInboundBatchTotalsSqlFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public UpdateInboundBatchTotalsSqlFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<UpdateInboundBatchTotalsSql.Handler>()
                    .AddMock<ILogger<UpdateInboundBatchTotalsSql.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var command = new UpdateInboundBatchTotalsSql.Command(100, 100, 22.33m);
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.SaveExecuteAsync(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundBatch_Update_Totals]", StringComparison.CurrentCultureIgnoreCase)),
                Arg.Is<UpdateInboundBatchTotalsSql.Command>(_ => _ == command), Arg.Any<CancellationToken>())
                .Returns(1);

            var handler = _container.Get<UpdateInboundBatchTotalsSql.Handler>();
            Func<Task<Unit>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().SaveExecuteAsync(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundBatch_Update_Totals]", StringComparison.CurrentCultureIgnoreCase)),
               Arg.Is<UpdateInboundBatchTotalsSql.Command>(_ => _ == command), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfSaveFailsWithException()
        {
            var command = new UpdateInboundBatchTotalsSql.Command(100, 100, 22.33m);
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.When(_ => _.SaveExecuteAsync(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundBatch_Update_Totals]", StringComparison.CurrentCultureIgnoreCase)),
                Arg.Is<UpdateInboundBatchTotalsSql.Command>(_ => _ == command), Arg.Any<CancellationToken>()))
                 .Do(_ => throw new Exception());

            var handler = _container.Get<UpdateInboundBatchTotalsSql.Handler>();
            Func<Task<Unit>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().SaveExecuteAsync(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundBatch_Update_Totals]", StringComparison.CurrentCultureIgnoreCase)),
               Arg.Is<UpdateInboundBatchTotalsSql.Command>(_ => _ == command), Arg.Any<CancellationToken>());
        }
    }
}
