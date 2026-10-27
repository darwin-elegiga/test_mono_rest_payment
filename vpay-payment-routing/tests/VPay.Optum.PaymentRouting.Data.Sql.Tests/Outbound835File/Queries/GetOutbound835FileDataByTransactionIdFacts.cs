using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.Models;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Queries;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests.OutboundPayment.Queries
{
    public class GetOutbound835FileDataByTransactionIdFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public GetOutbound835FileDataByTransactionIdFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<GetOutbound835FileDataByTransactionId.Handler>()
                    .AddMock<ILogger<GetOutbound835FileDataByTransactionId.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var query = _container.Fake<GetOutbound835FileDataByTransactionId.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();
            var expectedResult = _container.Fake<Outbound835Data>();

            connection.GetFirstOrDefaultAsync<Outbound835Data>(Arg.Is<string>(_ => _.Equals("[dbo].[usp_Outbound835Data_GetByTransactionId]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetOutbound835FileDataByTransactionId.Query>(_ => _ == query), Arg.Any<CancellationToken>())
                .Returns(expectedResult);

            var handler = _container.Get<GetOutbound835FileDataByTransactionId.Handler>();
            Func<Task<Outbound835Data>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().GetFirstOrDefaultAsync<Outbound835Data>(Arg.Is<string>(_ => _.Equals("[dbo].[usp_Outbound835Data_GetByTransactionId]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetOutbound835FileDataByTransactionId.Query>(_ => _ == query), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfGetFirstOrDefaultFailsWithException()
        {
            var query = _container.Fake<GetOutbound835FileDataByTransactionId.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.When(_ => _.GetFirstOrDefaultAsync<Outbound835Data>(Arg.Is<string>(_ => _.Equals("[dbo].[usp_Outbound835Data_GetByTransactionId]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetOutbound835FileDataByTransactionId.Query>(_ => _ == query), Arg.Any<CancellationToken>()))
                .Do(_ => throw new Exception());

            var handler = _container.Get<GetOutbound835FileDataByTransactionId.Handler>();
            Func<Task<Outbound835Data>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().GetFirstOrDefaultAsync<Outbound835Data>(Arg.Is<string>(_ => _.Equals("[dbo].[usp_Outbound835Data_GetByTransactionId]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetOutbound835FileDataByTransactionId.Query>(_ => _ == query), Arg.Any<CancellationToken>());
        }
    }
}
