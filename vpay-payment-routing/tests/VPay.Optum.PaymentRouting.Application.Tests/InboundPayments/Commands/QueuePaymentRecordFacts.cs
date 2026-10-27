using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Optum.PaymentRouting.Application.InboundPayments.Commands;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Application.Tests.InboundPayments.Commands
{
    public class QueuePaymentRecordFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public QueuePaymentRecordFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddMock<ILogger<QueuePaymentRecord.Handler>>()
                    .AddMock<IPublishEndpoint>()
                    .AddSingleton<QueuePaymentRecord.Handler>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            // Arrange
            PaymentRecord paymentRecord = _container.Fake<PaymentRecord>();
            var command = new QueuePaymentRecord.Command { PaymentRecord = paymentRecord };
            var publishEndpoint = _container.Get<IPublishEndpoint>();
            publishEndpoint.Publish(Arg.Is<PaymentRecord>(_ => _ == command.PaymentRecord), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            var handler = _container.Get<QueuePaymentRecord.Handler>();
            Func<Task<Unit>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().NotThrowAsync();
            await publishEndpoint.Received().Publish(Arg.Is<PaymentRecord>(_ => _ == command.PaymentRecord), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfPublishFailsWithException()
        {
            PaymentRecord paymentRecord = _container.Fake<PaymentRecord>();
            var command = new QueuePaymentRecord.Command { PaymentRecord = paymentRecord };
            var publishEndpoint = _container.Get<IPublishEndpoint>();
            publishEndpoint.When(_ => _.Publish(Arg.Is<PaymentRecord>(_ => _ == command.PaymentRecord), Arg.Any<CancellationToken>()))
                .Do(_ => throw new Exception());

            var handler = _container.Get<QueuePaymentRecord.Handler>();
            Func<Task<Unit>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();
            await publishEndpoint.Received().Publish(Arg.Is<PaymentRecord>(_ => _ == command.PaymentRecord), Arg.Any<CancellationToken>());
        }
    }
}
