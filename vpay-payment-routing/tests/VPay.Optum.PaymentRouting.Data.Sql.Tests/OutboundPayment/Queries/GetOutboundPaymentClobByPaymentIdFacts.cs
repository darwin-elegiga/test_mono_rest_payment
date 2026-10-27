using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.OutboundPayment.Queries;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests.OutboundPayment.Queries
{
    public class GetOutboundPaymentClobByPaymentIdFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public GetOutboundPaymentClobByPaymentIdFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<GetOutboundPaymentClobByPaymentId.Handler>()
                    .AddMock<ILogger<GetOutboundPaymentClobByPaymentId.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var query = _container.Fake<GetOutboundPaymentClobByPaymentId.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();
            var expectedResult = _container.Fake<GetOutboundPaymentClobByPaymentId.Result>();

            connection.GetFirstOrDefaultAsync<GetOutboundPaymentClobByPaymentId.Result>(Arg.Is<string>(_ => _.Equals("dbo.[usp_OutboundPaymentClob_GetBy_InboundPaymentId]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetOutboundPaymentClobByPaymentId.Query>(_ => _ == query), Arg.Any<CancellationToken>())
                .Returns(expectedResult);

            var handler = _container.Get<GetOutboundPaymentClobByPaymentId.Handler>();
            Func<Task<GetOutboundPaymentClobByPaymentId.Result>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().GetFirstOrDefaultAsync<GetOutboundPaymentClobByPaymentId.Result>(Arg.Is<string>(_ => _.Equals("dbo.[usp_OutboundPaymentClob_GetBy_InboundPaymentId]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetOutboundPaymentClobByPaymentId.Query>(_ => _ == query), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfGetFirstOrDefaultFailsWithException()
        {
            var query = _container.Fake<GetOutboundPaymentClobByPaymentId.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.When(_ => _.GetFirstOrDefaultAsync<GetOutboundPaymentClobByPaymentId.Result>(Arg.Is<string>(_ => _.Equals("dbo.[usp_OutboundPaymentClob_GetBy_InboundPaymentId]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetOutboundPaymentClobByPaymentId.Query>(_ => _ == query), Arg.Any<CancellationToken>()))
                .Do(_ => throw new Exception());

            var handler = _container.Get<GetOutboundPaymentClobByPaymentId.Handler>();
            Func<Task<GetOutboundPaymentClobByPaymentId.Result>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().GetFirstOrDefaultAsync<GetOutboundPaymentClobByPaymentId.Result>(Arg.Is<string>(_ => _.Equals("dbo.[usp_OutboundPaymentClob_GetBy_InboundPaymentId]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetOutboundPaymentClobByPaymentId.Query>(_ => _ == query), Arg.Any<CancellationToken>());
        }
    }
}
