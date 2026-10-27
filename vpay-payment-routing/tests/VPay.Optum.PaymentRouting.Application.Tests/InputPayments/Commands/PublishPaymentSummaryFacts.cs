using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Optum.PaymentRouting.Application.InboundPayments.Commands;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Application.Tests.InputPayments.Commands
{
    public class PublishPaymentSummaryFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public PublishPaymentSummaryFacts()
        {
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddMock<ILogger<PublishPaymentSummary.Handler>>()
                    .AddMock<IPublishEndpoint>()
                    .AddSingleton<PublishPaymentSummary.Handler>();
            });
        }

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var command = _container.Fake<PublishPaymentSummary.Command>();
            var publishEndpoint = _container.Get<IPublishEndpoint>();
            publishEndpoint.Publish(Arg.Is<PaymentSummary>(_ => _ == command.PaymentSummary), Arg.Any<CancellationToken>())
                 .Returns(Task.CompletedTask);
            var handler = _container.Get<PublishPaymentSummary.Handler>();
            Func<Task> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().NotThrowAsync();
            await publishEndpoint.Received().Publish(Arg.Is<PaymentSummary>(_ => _ == command.PaymentSummary), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfSendFailsWithException()
        {
            var command = _container.Fake<PublishPaymentSummary.Command>();
            var publishEndpoint = _container.Get<IPublishEndpoint>();
            publishEndpoint.When(_ => _.Publish(Arg.Is<PaymentSummary>(_ => _ == command.PaymentSummary), Arg.Any<CancellationToken>()))
                .Do(_ => throw new Exception());

            var handler = _container.Get<PublishPaymentSummary.Handler>();
            Func<Task> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();
            await publishEndpoint.Received().Publish(Arg.Is<PaymentSummary>(_ => _ == command.PaymentSummary), Arg.Any<CancellationToken>());
        }
    }
}
