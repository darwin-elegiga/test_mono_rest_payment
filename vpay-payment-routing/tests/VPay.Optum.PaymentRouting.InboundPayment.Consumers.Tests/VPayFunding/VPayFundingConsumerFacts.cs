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
using VPay.Optum.PaymentRouting.InboundPayment.Consumers.Consumers.VPayFunding;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.InboundPayment.Consumers.Tests.VPayFunding
{
    public class VPayFundingConsumerFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public VPayFundingConsumerFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<VpayFundingConsumer>()
                    .AddMock<ILogger<VpayFundingConsumer>>()
                    .AddMock<IBus>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var vpayFunding = new VpayFunding
            {
                batchID = "111",
                payer835ID = "22112",
                totalPaymentAmount = new byte[] { 123, 22 },
                totalPaymentCount = 22
            };

            var consumer = _container.Get<VpayFundingConsumer>();
            Func<Task> act = () => consumer.Consume(vpayFunding, CancellationToken.None);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ConsumeWithNoMessage_ThrowsException()
        {
            var provider = new ServiceCollection()
                .AddLogging(l => l.AddProvider(NullLoggerProvider.Instance))
                .AddMassTransitInMemoryTestHarness(cfg =>
                {
                    cfg.AddConsumer<VpayFundingConsumer>();
                    cfg.AddConsumerTestHarness<VpayFundingConsumer>();
                })
                .BuildServiceProvider(true);
            var harness = provider.GetRequiredService<InMemoryTestHarness>();
            try
            {
                await harness.Start();
                var bus = provider.GetRequiredService<IBus>();

                IRequestClient<VpayFundingConsumer> client = bus.CreateRequestClient<VpayFundingConsumer>();

                Func<Task> act = () => client.GetResponse<VpayFundingConsumer>(null);

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
