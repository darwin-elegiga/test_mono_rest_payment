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
using VPay.Optum.PaymentRouting.Data.Sql.InboundHeader.Queries;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests.InboundHeaders.Queries
{
    public class GetInboundHeaderByBatchIdFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public GetInboundHeaderByBatchIdFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<GetInboundHeaderByBatchId.Handler>()
                    .AddMock<ILogger<GetInboundHeaderByBatchId.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var expectedResult = _container.Fake<Models.InboundHeader>();
            var command = _container.Fake<GetInboundHeaderByBatchId.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.GetFirstOrDefaultAsync<Models.InboundHeader>(Arg.Is<string>(_ => _.Equals("dbo.usp_InboundHeader_GetByBatchId", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetInboundHeaderByBatchId.Query>(_ => _ == command), Arg.Any<CancellationToken>())
                .Returns(expectedResult);

            var handler = _container.Get<GetInboundHeaderByBatchId.Handler>();
            Func<Task<Models.InboundHeader>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().GetFirstOrDefaultAsync<Models.InboundHeader>(Arg.Is<string>(_ => _.Equals("dbo.usp_InboundHeader_GetByBatchId", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetInboundHeaderByBatchId.Query>(_ => _ == command), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfSavesFailsWithException()
        {
            var command = _container.Fake<GetInboundHeaderByBatchId.Query>();
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            connection.When(_ => _.GetFirstOrDefaultAsync<Models.InboundHeader>(Arg.Is<string>(_ => _.Equals("dbo.usp_InboundHeader_GetByBatchId", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetInboundHeaderByBatchId.Query>(_ => _ == command), Arg.Any<CancellationToken>()))
                .Do(_ => throw new Exception());

            var handler = _container.Get<GetInboundHeaderByBatchId.Handler>();
            Func<Task<Models.InboundHeader>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().GetFirstOrDefaultAsync<Models.InboundHeader>(Arg.Is<string>(_ => _.Equals("dbo.usp_InboundHeader_GetByBatchId", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<GetInboundHeaderByBatchId.Query>(_ => _ == command), Arg.Any<CancellationToken>());
        }
    }
}
