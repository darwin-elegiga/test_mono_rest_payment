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
using VPay.Optum.PaymentRouting.Data.Sql.OutboundPayment.Commands;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests.OutboundPayment.Commands
{
    public class InsertOutboundPaymentClobSqlFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public InsertOutboundPaymentClobSqlFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<InsertOutboundPaymentClobSql.Handler>()
                    .AddMock<ILogger<InsertOutboundPaymentClobSql.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var command = _container.Fake<InsertOutboundPaymentClobSql.Command>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.SaveExecuteAsync(Arg.Is<string>(_ => _.Equals("[dbo].[usp_OutboundPaymentClob_Insert]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<InsertOutboundPaymentClobSql.Command>(_ => _ == command), Arg.Any<CancellationToken>())
                .Returns(1);

            var handler = _container.Get<InsertOutboundPaymentClobSql.Handler>();
            Func<Task<Unit>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().SaveExecuteAsync(Arg.Is<string>(_ => _.Equals("[dbo].[usp_OutboundPaymentClob_Insert]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<InsertOutboundPaymentClobSql.Command>(_ => _ == command), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfSavesFailsWithException()
        {
            var command = _container.Fake<InsertOutboundPaymentClobSql.Command>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.When(_ => _.SaveExecuteAsync(Arg.Is<string>(_ => _.Equals("[dbo].[usp_OutboundPaymentClob_Insert]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<InsertOutboundPaymentClobSql.Command>(_ => _ == command), Arg.Any<CancellationToken>()))
                .Do(_ => throw new Exception());

            var handler = _container.Get<InsertOutboundPaymentClobSql.Handler>();
            Func<Task<Unit>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().SaveExecuteAsync(Arg.Is<string>(_ => _.Equals("[dbo].[usp_OutboundPaymentClob_Insert]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<InsertOutboundPaymentClobSql.Command>(_ => _ == command), Arg.Any<CancellationToken>());
        }
    }
}
