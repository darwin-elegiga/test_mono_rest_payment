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
    public class GetInboundPaymentWithClobByIdSqlFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public GetInboundPaymentWithClobByIdSqlFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<GetInboundPaymentWithClobByIdSql.Handler>()
                    .AddMock<ILogger<GetInboundPaymentWithClobByIdSql.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var query = _container.Fake<GetInboundPaymentWithClobByIdSql.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.GetFirstOrDefaultAsync<GetInboundPaymentWithClobByIdSql.Result>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundPayment_GetById]", StringComparison.CurrentCultureIgnoreCase)),
                Arg.Is<GetInboundPaymentWithClobByIdSql.Query>(_ => _ == query), Arg.Any<CancellationToken>())
                .Returns(_container.Fake<GetInboundPaymentWithClobByIdSql.Result>());

            var handler = _container.Get<GetInboundPaymentWithClobByIdSql.Handler>();
            Func<Task<GetInboundPaymentWithClobByIdSql.Result>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().GetFirstOrDefaultAsync<GetInboundPaymentWithClobByIdSql.Result>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundPayment_GetById]", StringComparison.CurrentCultureIgnoreCase)),
               Arg.Is<GetInboundPaymentWithClobByIdSql.Query>(_ => _ == query), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfSaveFailsWithException()
        {
            var query = _container.Fake<GetInboundPaymentWithClobByIdSql.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.When(_ => _.GetFirstOrDefaultAsync<GetInboundPaymentWithClobByIdSql.Result>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundPayment_GetById]", StringComparison.CurrentCultureIgnoreCase)),
                Arg.Is<GetInboundPaymentWithClobByIdSql.Query>(_ => _ == query), Arg.Any<CancellationToken>()))
                 .Do(_ => throw new Exception());

            var handler = _container.Get<GetInboundPaymentWithClobByIdSql.Handler>();
            Func<Task<GetInboundPaymentWithClobByIdSql.Result>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().GetFirstOrDefaultAsync<GetInboundPaymentWithClobByIdSql.Result>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundPayment_GetById]", StringComparison.CurrentCultureIgnoreCase)),
               Arg.Is<GetInboundPaymentWithClobByIdSql.Query>(_ => _ == query), Arg.Any<CancellationToken>());
        }
    }
}
