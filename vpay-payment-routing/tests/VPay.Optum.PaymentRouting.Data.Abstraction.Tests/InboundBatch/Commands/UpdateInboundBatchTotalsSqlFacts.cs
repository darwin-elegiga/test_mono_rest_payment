using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.InboundBatch.Commands;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Abstraction.Tests.InboundBatch.Commands
{
    public class UpdateInboundBatchTotalsSqlFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public UpdateInboundBatchTotalsSqlFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
                services
                    .AddMock<ILogger<UpdateInboundBatchTotalsSql.Handler>>()
                    .AddSingleton(new Mock<IDataConnection<SqlConnection>>(MockBehavior.Strict).Object)
                    .AddSingleton<UpdateInboundBatchTotalsSql.Handler>()
            );

        [Fact]
        public async Task Handle_ShouldThrowMockExceptionOnly()
        {
            // arrange
            var batchId = _container.Fake<long>();
            var count = _container.Fake<int>();
            var amount = _container.Fake<decimal>();
            var command = new UpdateInboundBatchTotalsSql.Command(batchId, count, amount);
            var handler = _container.Get<UpdateInboundBatchTotalsSql.Handler>();

            // act
            async Task act() => await handler.Handle(command, CancellationToken.None);

            // assert
            await Assert.ThrowsAsync<MockException>(act); // throws on connect
        }
    }
}
