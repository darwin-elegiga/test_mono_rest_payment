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
    public class GetInboundPaymentByUserKeyPayer835IdSqlFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public GetInboundPaymentByUserKeyPayer835IdSqlFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<GetInboundPaymentByUserKeyPayer835IdSql.Handler>()
                    .AddMock<ILogger<GetInboundPaymentByUserKeyPayer835IdSql.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var query = _container.Fake<GetInboundPaymentByUserKeyPayer835IdSql.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.GetFirstOrDefaultAsync<GetInboundPaymentByUserKeyPayer835IdSql.Result>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundPayment_GetBy_UserKey_Payer835Id_MetaDataOnly]", StringComparison.CurrentCultureIgnoreCase)),
                Arg.Is<GetInboundPaymentByUserKeyPayer835IdSql.Query>(_ => _ == query), Arg.Any<CancellationToken>())
                .Returns(_container.Fake<GetInboundPaymentByUserKeyPayer835IdSql.Result>());

            var handler = _container.Get<GetInboundPaymentByUserKeyPayer835IdSql.Handler>();
            Func<Task<GetInboundPaymentByUserKeyPayer835IdSql.Result>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().GetFirstOrDefaultAsync<GetInboundPaymentByUserKeyPayer835IdSql.Result>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundPayment_GetBy_UserKey_Payer835Id_MetaDataOnly]", StringComparison.CurrentCultureIgnoreCase)),
               Arg.Is<GetInboundPaymentByUserKeyPayer835IdSql.Query>(_ => _ == query), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfSaveFailsWithException()
        {
            var query = _container.Fake<GetInboundPaymentByUserKeyPayer835IdSql.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.When(_ => _.GetFirstOrDefaultAsync<GetInboundPaymentByUserKeyPayer835IdSql.Result>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundPayment_GetBy_UserKey_Payer835Id_MetaDataOnly]", StringComparison.CurrentCultureIgnoreCase)),
                Arg.Is<GetInboundPaymentByUserKeyPayer835IdSql.Query>(_ => _ == query), Arg.Any<CancellationToken>()))
                 .Do(_ => throw new Exception());

            var handler = _container.Get<GetInboundPaymentByUserKeyPayer835IdSql.Handler>();
            Func<Task<GetInboundPaymentByUserKeyPayer835IdSql.Result>> act = () => handler.Handle(query, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().GetFirstOrDefaultAsync<GetInboundPaymentByUserKeyPayer835IdSql.Result>(Arg.Is<string>(_ => _.Equals("dbo.[usp_InboundPayment_GetBy_UserKey_Payer835Id_MetaDataOnly]", StringComparison.CurrentCultureIgnoreCase)),
               Arg.Is<GetInboundPaymentByUserKeyPayer835IdSql.Query>(_ => _ == query), Arg.Any<CancellationToken>());
        }
    }
}
