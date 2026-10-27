using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Application.InboundPayments.Commands;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Application.Tests.InboundPayments.Commands
{
    public class CompleteInboundBatchFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public CompleteInboundBatchFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<CompleteInboundBatch.Handler>()
                    .AddMock<IMediator>()
                    .AddMock<ILogger<CompleteInboundBatch.Handler>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            //Arrange
            long batchId = _container.Fake<long>();

            var command = new CompleteInboundBatch.Command {InboundBatchId = batchId };
            CompleteInboundBatch.Handler handler = _container.Get<CompleteInboundBatch.Handler>();

            //Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);


            //Assert
            await act.Should().NotThrowAsync();
        }
    }
}
