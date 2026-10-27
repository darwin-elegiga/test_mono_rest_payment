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
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Commands;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests
{
    public class UpdateInboundPaymentRefEoSqlSqlFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public UpdateInboundPaymentRefEoSqlSqlFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<UpdateInboundPaymentRefEoSql.Handler>()
                    .AddMock<ILogger<UpdateInboundPaymentRefEoSql.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var command = _container.Fake<UpdateInboundPaymentRefEoSql.Command>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.SaveExecuteAsync(Arg.Is<string>(_ => _.Equals("[dbo].[usp_InboundPayment_Update_RefEo]", StringComparison.CurrentCultureIgnoreCase)),
                Arg.Is<UpdateInboundPaymentRefEoSql.Command>(_ => _ == command), Arg.Any<CancellationToken>())
                .Returns(1);

            var handler = _container.Get<UpdateInboundPaymentRefEoSql.Handler>();
            Func<Task<Unit>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().SaveExecuteAsync(Arg.Is<string>(_ => _.Equals("[dbo].[usp_InboundPayment_Update_RefEo]", StringComparison.CurrentCultureIgnoreCase)),
               Arg.Is<UpdateInboundPaymentRefEoSql.Command>(_ => _ == command), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfSaveFailsWithException()
        {
            var command = _container.Fake<UpdateInboundPaymentRefEoSql.Command>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.When(_ => _.SaveExecuteAsync(Arg.Is<string>(_ => _.Equals("[dbo].[usp_InboundPayment_Update_RefEo]", StringComparison.CurrentCultureIgnoreCase)),
                Arg.Is<UpdateInboundPaymentRefEoSql.Command>(_ => _ == command), Arg.Any<CancellationToken>()))
                 .Do(_ => throw new Exception());

            var handler = _container.Get<UpdateInboundPaymentRefEoSql.Handler>();
            Func<Task<Unit>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().SaveExecuteAsync(Arg.Is<string>(_ => _.Equals("[dbo].[usp_InboundPayment_Update_RefEo]", StringComparison.CurrentCultureIgnoreCase)),
               Arg.Is<UpdateInboundPaymentRefEoSql.Command>(_ => _ == command), Arg.Any<CancellationToken>());
        }
    }
}
