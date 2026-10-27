using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Application.InboundPayments.Commands;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Application.Tests.InboundPayments.Commands
{
    public class PublishPaymentSummaryFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public PublishPaymentSummaryFacts()
        {
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<PublishPaymentSummary.Handler>()
                    .AddMock<IPublishEndpoint>()
                    .AddMock<ILogger<PublishPaymentSummary.Handler>>();
            });
        }

        [Fact]
        public async Task ShouldNotThrow()
        {
            var command = _container.Fake<PublishPaymentSummary.Command>();

            var handler = _container.Get<PublishPaymentSummary.Handler>();
            await handler.Handle(command, CancellationToken.None);

            //This publish message is an extension method and NSubstitute cannot mock it properly
            //var publishEndpoint = _container.Get<IPublishEndpoint>();
            //await publishEndpoint.Received().Publish(Arg.Is<PaymentSummary>(x => x == command.PaymentSummary),
            //    Arg.Is<Action<PublishContext<PaymentSummary>>>(x => true), Arg.Any<CancellationToken>());
        }
    }
}
