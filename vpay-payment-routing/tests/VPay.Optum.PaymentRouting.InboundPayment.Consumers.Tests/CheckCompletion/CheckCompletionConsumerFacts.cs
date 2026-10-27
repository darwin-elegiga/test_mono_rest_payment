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
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.InboundPayment.Consumers.Consumers.CheckCompletion;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.InboundPayment.Consumers.Tests.CheckCompletion
{
    public class CheckCompletionConsumerFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public CheckCompletionConsumerFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<CheckCompletionConsumer>()
                    .AddMock<ILogger<CheckCompletionConsumer>>()
                    .AddMock<IMediator>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var batchCompletionMessage = new CheckBatchCompletionMessage(100);

            var consumer = _container.Get<CheckCompletionConsumer>();
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
                    cfg.AddConsumer<CheckCompletionConsumer>();
                    cfg.AddConsumerTestHarness<CheckCompletionConsumer>();
                })
                .BuildServiceProvider(true);
            var harness = provider.GetRequiredService<InMemoryTestHarness>();
            try
            {
                await harness.Start();
                var bus = provider.GetRequiredService<IBus>();

                IRequestClient<CheckCompletionConsumer> client = bus.CreateRequestClient<CheckCompletionConsumer>();

                Func<Task> act = () => client.GetResponse<CheckCompletionConsumer>(null);

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
