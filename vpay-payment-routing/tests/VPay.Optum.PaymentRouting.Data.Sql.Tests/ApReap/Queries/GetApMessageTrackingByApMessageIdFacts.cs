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
using VPay.Optum.PaymentRouting.Data.Sql.ApReap.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.Models;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests.ApReap.Queries
{
    public class GetApMessageTrackingByApMessageIdFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public GetApMessageTrackingByApMessageIdFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<GetApMessageTrackingByApMessageId.Handler>()
                    .AddMock<ILogger<GetApMessageTrackingByApMessageId.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var query = _container.Fake<GetApMessageTrackingByApMessageId.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();
            var expectedResult = _container.Fake<ApMessageTracking>(_ => _.With(x => x.Status, ApMessageTrackingStatus.Queued));

            connection.GetFirstOrDefaultAsync<ApMessageTracking>(Arg.Is<string>(_ => _.Equals("[dbo].[usp_ApMessageTracking_GetByApMessageId]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetApMessageTrackingByApMessageId.Query>(_ => _ == query), Arg.Any<CancellationToken>())
                .Returns(expectedResult);

            var handler = _container.Get<GetApMessageTrackingByApMessageId.Handler>();
            Func<Task<ApMessageTracking>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().GetFirstOrDefaultAsync<ApMessageTracking>(Arg.Is<string>(_ => _.Equals("[dbo].[usp_ApMessageTracking_GetByApMessageId]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetApMessageTrackingByApMessageId.Query>(_ => _ == query), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfGetFirstOrDefaultFailsWithException()
        {
            var query = _container.Fake<GetApMessageTrackingByApMessageId.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.When(_ => _.GetFirstOrDefaultAsync<ApMessageTracking>(Arg.Is<string>(_ => _.Equals("[dbo].[usp_ApMessageTracking_GetByApMessageId]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetApMessageTrackingByApMessageId.Query>(_ => _ == query), Arg.Any<CancellationToken>()))
                .Do(_ => throw new Exception());

            var handler = _container.Get<GetApMessageTrackingByApMessageId.Handler>();
            Func<Task<ApMessageTracking>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().GetFirstOrDefaultAsync<ApMessageTracking>(Arg.Is<string>(_ => _.Equals("[dbo].[usp_ApMessageTracking_GetByApMessageId]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetApMessageTrackingByApMessageId.Query>(_ => _ == query), Arg.Any<CancellationToken>());
        }
    }
}
