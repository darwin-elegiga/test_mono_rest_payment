using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.InboundPayment.Consumers.Consumers.VPayPayment;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.InboundPayment.Consumers.Tests.VPayPayment
{
    public class VPayPaymentConsumerFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public VPayPaymentConsumerFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<VpayPaymentConsumer>()
                    .AddMock<ILogger<VpayPaymentConsumer>>()
                    .AddMock<IBus>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var vpayPayment = _container.Fake<VpayPayment>();

            var consumer = _container.Get<VpayPaymentConsumer>();
            Func<Task> act = () => consumer.Consume(vpayPayment, CancellationToken.None);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ConsumeWithNoMessage_ThrowsException()
        {
            var provider = new ServiceCollection()
                .AddLogging(l => l.AddProvider(NullLoggerProvider.Instance))
                .AddMassTransitInMemoryTestHarness(cfg =>
                {
                    cfg.AddConsumer<VpayPaymentConsumer>();
                    cfg.AddConsumerTestHarness<VpayPaymentConsumer>();
                })
                .BuildServiceProvider(true);
            var harness = provider.GetRequiredService<InMemoryTestHarness>();
            try
            {
                await harness.Start();
                var bus = provider.GetRequiredService<IBus>();

                IRequestClient<VpayPaymentConsumer> client = bus.CreateRequestClient<VpayPaymentConsumer>();

                Func<Task> act = () => client.GetResponse<VpayPaymentConsumer>(null);

                await act.Should().ThrowAsync<RequestException>();
            }
            finally
            {
                await harness.Stop();
                await provider.DisposeAsync();
            }
        }
    }
}
