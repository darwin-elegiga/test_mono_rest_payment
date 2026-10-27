using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VPay.Optum.PaymentRouting.InboundPayment.Consumers.Consumers;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.InboundPayment.Consumers.Tests.PaymentSummary
{
    public class PaymentSummaryConsumerFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public PaymentSummaryConsumerFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<PaymentSummaryConsumer>()
                    .AddMock<ILogger<PaymentSummaryConsumer>>()
                    .AddMock<IMediator>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var batchCompletionMessage = new Common.Serialization.PaymentSummary
            {
                BatchId = "123",
                Payer835Id = "122",
                TotalPaymentAmount = "12211.22",
                TotalPaymentCount = "1223"
            };

            var consumer = _container.Get<PaymentSummaryConsumer>();
            Func<Task> act = () => consumer.Consume(batchCompletionMessage, CancellationToken.None);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ConsumeWithNoMessage_ThrowsException()
        {
            var provider = new ServiceCollection()
                .AddLogging(l => l.AddProvider(NullLoggerProvider.Instance))
                .AddMassTransitInMemoryTestHarness(cfg =>
                {
                    cfg.AddConsumer<PaymentSummaryConsumer>();
                    cfg.AddConsumerTestHarness<PaymentSummaryConsumer>();
                })
                .BuildServiceProvider(true);
            var harness = provider.GetRequiredService<InMemoryTestHarness>();
            try
            {
                await harness.Start();
                var bus = provider.GetRequiredService<IBus>();

                IRequestClient<PaymentSummaryConsumer> client = bus.CreateRequestClient<PaymentSummaryConsumer>();

                Func<Task> act = () => client.GetResponse<PaymentSummaryConsumer>(null);

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
