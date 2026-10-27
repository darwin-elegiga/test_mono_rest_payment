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
using VPay.Optum.PaymentRouting.Data.Sql.InboundHeader;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests.InboundHeaders.Queries
{
    public class GetInboundHeaderByPaymentIdSqlFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public GetInboundHeaderByPaymentIdSqlFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<GetInboundHeaderByPaymentIdSql.Handler>()
                    .AddMock<ILogger<GetInboundHeaderByPaymentIdSql.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var expectedResult = _container.Fake<InboundHeader.InboundHeader>(_ => _.With(_ => _.Status, PaymentStatus.Received));
            var command = _container.Fake<GetInboundHeaderByPaymentIdSql.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.GetFirstOrDefaultAsync<InboundHeader.InboundHeader>(Arg.Is<string>(_ => _.Equals("[dbo].[usp_InboundHeader_GetByInboundPaymentId]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetInboundHeaderByPaymentIdSql.Query>(_ => _ == command), Arg.Any<CancellationToken>())
                .Returns(expectedResult);

            var handler = _container.Get<GetInboundHeaderByPaymentIdSql.Handler>();
            Func<Task<InboundHeader.InboundHeader>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().GetFirstOrDefaultAsync<InboundHeader.InboundHeader>(Arg.Is<string>(_ => _.Equals("[dbo].[usp_InboundHeader_GetByInboundPaymentId]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetInboundHeaderByPaymentIdSql.Query>(_ => _ == command), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfSavesFailsWithException()
        {
            var command = _container.Fake<GetInboundHeaderByPaymentIdSql.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.When(_ => _.GetFirstOrDefaultAsync<InboundHeader.InboundHeader>(Arg.Is<string>(_ => _.Equals("[dbo].[usp_InboundHeader_GetByInboundPaymentId]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetInboundHeaderByPaymentIdSql.Query>(_ => _ == command), Arg.Any<CancellationToken>()))
                .Do(_ => throw new Exception());

            var handler = _container.Get<GetInboundHeaderByPaymentIdSql.Handler>();
            Func<Task<InboundHeader.InboundHeader>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().GetFirstOrDefaultAsync<InboundHeader.InboundHeader>(Arg.Is<string>(_ => _.Equals("[dbo].[usp_InboundHeader_GetByInboundPaymentId]", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetInboundHeaderByPaymentIdSql.Query>(_ => _ == command), Arg.Any<CancellationToken>());
        }
    }
}
