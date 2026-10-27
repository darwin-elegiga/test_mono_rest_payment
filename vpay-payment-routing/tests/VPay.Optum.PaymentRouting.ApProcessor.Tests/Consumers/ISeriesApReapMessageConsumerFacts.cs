using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Optum.PaymentRouting.ApProcessor.Consumers;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.ApProcessor.Tests.Consumers
{
    public class ISeriesApReapMessageConsumerFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public ISeriesApReapMessageConsumerFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<ISeriesApReapMessageConsumer>()
                    .AddMock<ILogger<ISeriesApReapMessageConsumer>>()
                    .AddMock<IMediator>();
            });

        [Fact]
        public async Task Should_WorkAsExpected()
        {
            var consumer = _container.Get<ISeriesApReapMessageConsumer>();
            var mediator = _container.Get<IMediator>();
            var message = _container.Fake<ApReapMessage>();
            mediator.Send(Arg.Is<ConsumeApMessage.Command>(_ => _.Message == message)).Returns((VpayApReap)null);

            await consumer.Consume(message);
            await mediator.Received().Send(Arg.Is<ConsumeApMessage.Command>(_ => _.Message == message));
        }
    }
}
