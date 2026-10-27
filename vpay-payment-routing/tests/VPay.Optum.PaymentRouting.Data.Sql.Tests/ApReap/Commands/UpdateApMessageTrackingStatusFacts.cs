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
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.ApReap.Commands;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests.ApReap.Commands
{
    public class UpdateApMessageTrackingStatusFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public UpdateApMessageTrackingStatusFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<UpdateApMessageTrackingStatus.Handler>()
                    .AddMock<ILogger<UpdateApMessageTrackingStatus.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var command = _container.Fake<UpdateApMessageTrackingStatus.Command>(_ => _.With(x => x.Status, ApMessageTrackingStatus.Queued));
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.SaveFirstOrDefaultAsync<Unit>(Arg.Is<string>(_ => _.Equals("dbo.usp_ApMessageTrackingStatus_UpdateStatus", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<UpdateApMessageTrackingStatus.Command>(_ => _ == command), Arg.Any<CancellationToken>())
                .Returns(Unit.Value);

            var handler = _container.Get<UpdateApMessageTrackingStatus.Handler>();
            Func<Task<Unit>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().SaveFirstOrDefaultAsync<Unit>(Arg.Is<string>(_ => _.Equals("dbo.usp_ApMessageTrackingStatus_UpdateStatus", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<UpdateApMessageTrackingStatus.Command>(_ => _ == command), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfSavesFailsWithException()
        {
            var command = _container.Fake<UpdateApMessageTrackingStatus.Command>(_ => _.With(x => x.Status, ApMessageTrackingStatus.Queued));
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.When(_ => _.SaveFirstOrDefaultAsync<Unit>(Arg.Is<string>(_ => _.Equals("dbo.usp_ApMessageTrackingStatus_UpdateStatus", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<UpdateApMessageTrackingStatus.Command>(_ => _ == command), Arg.Any<CancellationToken>()))
                .Do(_ => throw new Exception());

            var handler = _container.Get<UpdateApMessageTrackingStatus.Handler>();
            Func<Task<Unit>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().SaveFirstOrDefaultAsync<Unit>(Arg.Is<string>(_ => _.Equals("dbo.usp_ApMessageTrackingStatus_UpdateStatus", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<UpdateApMessageTrackingStatus.Command>(_ => _ == command), Arg.Any<CancellationToken>());
        }
    }
}
