using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests
{
    public class GetInboundPaymentWithClobByStatusFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public GetInboundPaymentWithClobByStatusFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<GetInboundPaymentWithClobByStatus.Handler>()
                    .AddMock<ILogger<GetInboundPaymentWithClobByStatus.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var query = new GetInboundPaymentWithClobByStatus.Query(PaymentStatus.ApSentSkip);
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.GetAsync<GetInboundPaymentWithClobByStatus.Result>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundPayment_GetByStatus]", StringComparison.CurrentCultureIgnoreCase)),
                Arg.Is<GetInboundPaymentWithClobByStatus.Query>(_ => _ == query), Arg.Any<CancellationToken>())
                .Returns(_container.FakeMany<GetInboundPaymentWithClobByStatus.Result>(_ => _.With(_ => _.Status, PaymentStatus.ApSentSkip)));

            var handler = _container.Get<GetInboundPaymentWithClobByStatus.Handler>();
            Func<Task<IEnumerable<GetInboundPaymentWithClobByStatus.Result>>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().GetAsync<GetInboundPaymentWithClobByStatus.Result>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundPayment_GetByStatus]", StringComparison.CurrentCultureIgnoreCase)),
               Arg.Is<GetInboundPaymentWithClobByStatus.Query>(_ => _ == query), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfSaveFailsWithException()
        {
            var query = new GetInboundPaymentWithClobByStatus.Query(PaymentStatus.ApSentSkip);
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.When(_ => _.GetAsync<GetInboundPaymentWithClobByStatus.Result>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundPayment_GetByStatus]", StringComparison.CurrentCultureIgnoreCase)),
                Arg.Is<GetInboundPaymentWithClobByStatus.Query>(_ => _ == query), Arg.Any<CancellationToken>()))
                 .Do(_ => throw new Exception());

            var handler = _container.Get<GetInboundPaymentWithClobByStatus.Handler>();
            Func<Task<IEnumerable<GetInboundPaymentWithClobByStatus.Result>>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().GetAsync<GetInboundPaymentWithClobByStatus.Result>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundPayment_GetByStatus]", StringComparison.CurrentCultureIgnoreCase)),
               Arg.Is<GetInboundPaymentWithClobByStatus.Query>(_ => _ == query), Arg.Any<CancellationToken>());
        }
    }
}
