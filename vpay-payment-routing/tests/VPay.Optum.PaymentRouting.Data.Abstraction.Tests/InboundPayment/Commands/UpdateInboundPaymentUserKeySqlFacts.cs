using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Commands;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Abstraction.Tests.InboundBatch.Commands
{
    public class UpdateInboundPaymentUserKeySqlFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public UpdateInboundPaymentUserKeySqlFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
                services
                    .AddMock<ILogger<UpdateInboundPaymentUserKeySql.Handler>>()
                    .AddSingleton(new Mock<IDataConnection<SqlConnection>>(MockBehavior.Strict).Object)
                    .AddSingleton<UpdateInboundPaymentUserKeySql.Handler>()
            );

        [Fact]
        public async Task Handle_ShouldThrowMockExceptionOnly()
        {
            // arrange
            var batchId = _container.Fake<long>();
            var userKey = _container.Fake<string>();
            var jiraIssueId = _container.Fake<string>();
            var command = new UpdateInboundPaymentUserKeySql.Command(batchId, userKey, jiraIssueId);
            var handler = _container.Get<UpdateInboundPaymentUserKeySql.Handler>();

            // act
            async Task act() => await handler.Handle(command, CancellationToken.None);

            // assert
            await Assert.ThrowsAsync<MockException>(act); // throws on connect
        }
    }
}
