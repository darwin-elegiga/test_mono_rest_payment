using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.ApReap.Commands;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Exceptions;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests.ApReap.Commands
{
    public class InsertApMessageTrackingFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public InsertApMessageTrackingFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<InsertApMessageTracking.Handler>()
                    .AddMock<ILogger<InsertApMessageTracking.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var command = _container.Fake<InsertApMessageTracking.Command>(_ => _.With(x => x.Status, ApMessageTrackingStatus.Queued));
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.SaveFirstOrDefaultAsync<long>(Arg.Is<string>(_ => _.Equals("dbo.usp_ApMessageTracking_Insert", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<InsertApMessageTracking.Command>(_ => _ == command), Arg.Any<CancellationToken>())
                .Returns(1);

            var handler = _container.Get<InsertApMessageTracking.Handler>();
            Func<Task<Result<long>>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().SaveFirstOrDefaultAsync<long>(Arg.Is<string>(_ => _.Equals("dbo.usp_ApMessageTracking_Insert", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<InsertApMessageTracking.Command>(_ => _ == command), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfSavesFailsWithException()
        {
            var command = _container.Fake<InsertApMessageTracking.Command>(_ => _.With(x => x.Status, ApMessageTrackingStatus.Queued));
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.When(_ => _.SaveFirstOrDefaultAsync<long>(Arg.Is<string>(_ => _.Equals("dbo.usp_ApMessageTracking_Insert", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<InsertApMessageTracking.Command>(_ => _ == command), Arg.Any<CancellationToken>()))
                .Do(_ => throw new Exception());

            var handler = _container.Get<InsertApMessageTracking.Handler>();
            Func<Task<Result<long>>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().SaveFirstOrDefaultAsync<long>(Arg.Is<string>(_ => _.Equals("dbo.usp_ApMessageTracking_Insert", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<InsertApMessageTracking.Command>(_ => _ == command), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_NotThrowError_WhenSavesFailsWithUniqueIndexViolationException()
        {
            var command = _container.Fake<InsertApMessageTracking.Command>(_ => _.With(x => x.Status, ApMessageTrackingStatus.Queued));
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            var sqlEx = SqlExceptionCreator.NewSqlException(SqlErrorCode.UNIQUE_INDEX_VIOLATION);
            connection.When(_ => _.SaveFirstOrDefaultAsync<long>(Arg.Is<string>(_ => _.Equals("dbo.usp_ApMessageTracking_Insert", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<InsertApMessageTracking.Command>(_ => _ == command), Arg.Any<CancellationToken>()))
                .Do(_ => throw sqlEx);

            var handler = _container.Get<InsertApMessageTracking.Handler>();
            Func<Task<Result<long>>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().SaveFirstOrDefaultAsync<long>(Arg.Is<string>(_ => _.Equals("dbo.usp_ApMessageTracking_Insert", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<InsertApMessageTracking.Command>(_ => _ == command), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowError_WhenSavesFailsWithSqlExceptionhOtherThanUniqueIndexViolationException()
        {
            var command = _container.Fake<InsertApMessageTracking.Command>(_ => _.With(x => x.Status, ApMessageTrackingStatus.Queued));
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            var sqlEx = SqlExceptionCreator.NewSqlException(SqlErrorCode.FOREIGN_KEY_REFERENCE_CONSTRAINT);
            connection.When(_ => _.SaveFirstOrDefaultAsync<long>(Arg.Is<string>(_ => _.Equals("dbo.usp_ApMessageTracking_Insert", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<InsertApMessageTracking.Command>(_ => _ == command), Arg.Any<CancellationToken>()))
                .Do(_ => throw sqlEx);

            var handler = _container.Get<InsertApMessageTracking.Handler>();
            Func<Task<Result<long>>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().SaveFirstOrDefaultAsync<long>(Arg.Is<string>(_ => _.Equals("dbo.usp_ApMessageTracking_Insert", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<InsertApMessageTracking.Command>(_ => _ == command), Arg.Any<CancellationToken>());
        }

    }
}
