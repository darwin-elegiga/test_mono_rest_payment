using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests
{
    public class GetInboundPaymentWithClobByUserKeyPayer835IdSqlFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public GetInboundPaymentWithClobByUserKeyPayer835IdSqlFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Handler>()
                    .AddMock<ILogger<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var query = _container.Fake<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.GetFirstOrDefaultAsync<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundPayment_GetBy_UserKey_Payer835Id]", StringComparison.CurrentCultureIgnoreCase)),
                Arg.Is<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Query>(_ => _ == query), Arg.Any<CancellationToken>())
                .Returns(_container.Fake<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result>());

            var handler = _container.Get<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Handler>();
            Func<Task<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().GetFirstOrDefaultAsync<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundPayment_GetBy_UserKey_Payer835Id]", StringComparison.CurrentCultureIgnoreCase)),
               Arg.Is<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Query>(_ => _ == query), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfSaveFailsWithException()
        {
            var query = _container.Fake<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.When(_ => _.GetFirstOrDefaultAsync<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundPayment_GetBy_UserKey_Payer835Id]", StringComparison.CurrentCultureIgnoreCase)),
                Arg.Is<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Query>(_ => _ == query), Arg.Any<CancellationToken>()))
                 .Do(_ => throw new Exception());

            var handler = _container.Get<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Handler>();
            Func<Task<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().GetFirstOrDefaultAsync<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundPayment_GetBy_UserKey_Payer835Id]", StringComparison.CurrentCultureIgnoreCase)),
               Arg.Is<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Query>(_ => _ == query), Arg.Any<CancellationToken>());
        }
    }
}
