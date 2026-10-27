using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Commands;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests
{
    public class UpdateOutbound835FileStatusFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public UpdateOutbound835FileStatusFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<UpdateOutbound835FileStatus.Handler>()
                    .AddMock<ILogger<UpdateOutbound835FileStatus.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var expectedResult = _container.Fake<Models.Outbound835File>(_ => _.With(_ => _.Status, Outbound835FileStatus.Queued));
            var command = new UpdateOutbound835FileStatus.Command(1, Outbound835FileStatus.Queued, "test", DateTime.Now, DateTime.Now.AddMinutes(-5));
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.SaveFirstOrDefaultAsync<Models.Outbound835File>(Arg.Is<string>(_ => _.Equals("[dbo].[usp_Outbound835File_UpdateStatus]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<UpdateOutbound835FileStatus.Command>(_ => _ == command), Arg.Any<CancellationToken>())
                .Returns(expectedResult);

            var handler = _container.Get<UpdateOutbound835FileStatus.Handler>();
            Func<Task<Models.Outbound835File>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().SaveFirstOrDefaultAsync<Models.Outbound835File>(Arg.Is<string>(_ => _.Equals("[dbo].[usp_Outbound835File_UpdateStatus]", StringComparison.CurrentCultureIgnoreCase)),
                Arg.Is<UpdateOutbound835FileStatus.Command>(_ => _ == command), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfSavesFailsWithException()
        {
            var command = new UpdateOutbound835FileStatus.Command(1, Outbound835FileStatus.Queued, "test", DateTime.Now, DateTime.Now.AddMinutes(-5));
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.When(_ => _.SaveFirstOrDefaultAsync<Models.Outbound835File>(Arg.Is<string>(_ => _.Equals("[dbo].[usp_Outbound835File_UpdateStatus]", StringComparison.CurrentCultureIgnoreCase)),
                Arg.Is<UpdateOutbound835FileStatus.Command>(_ => _ == command), Arg.Any<CancellationToken>()))
                 .Do(_ => throw new Exception());

            var handler = _container.Get<UpdateOutbound835FileStatus.Handler>();
            Func<Task<Models.Outbound835File>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().SaveFirstOrDefaultAsync<Models.Outbound835File>(Arg.Is<string>(_ => _.Equals("[dbo].[usp_Outbound835File_UpdateStatus]", StringComparison.CurrentCultureIgnoreCase)),
               Arg.Is<UpdateOutbound835FileStatus.Command>(_ => _ == command), Arg.Any<CancellationToken>());
        }
    }
}
