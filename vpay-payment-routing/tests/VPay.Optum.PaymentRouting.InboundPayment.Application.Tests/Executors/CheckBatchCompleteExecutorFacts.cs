using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Extensions.Exceptions;
using VPay.Optum.PaymentRouting.Application.Models.RabbitMessages;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;
using VPay.Optum.PaymentRouting.InboundPayment.Application.Executors;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.InboundPayment.Application.Tests.Executors
{
    public class CheckBatchCompleteExecutorFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public CheckBatchCompleteExecutorFacts()
        {
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<CheckBatchCompleteExecutor>()
                    .AddMock<ILogger<CheckBatchCompleteExecutor>>()
                    .AddMock<IMediator>()
                    .AddMock<IPublishEndpoint>();
            });
        }

        [Fact]
        public async Task ShouldHandleNotEnoughPayments()
        {
            var batchSummary = _container.Fake<BatchSummary>(x => x.With(y => y.Status, BatchStatus.BatchInitiated));
            batchSummary.ActualPaymentCount = batchSummary.TotalPaymentCount - 1;
            batchSummary.ActualPaymentAmount = batchSummary.TotalPaymentAmount;

            var executor = _container.Get<CheckBatchCompleteExecutor>();
            await executor.Execute(batchSummary, CancellationToken.None);

            var mediator = _container.Get<IMediator>();
            await mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<IRequest>(), Arg.Any<CancellationToken>());

            var publishEndpoint = _container.Get<IPublishEndpoint>();
            await publishEndpoint.DidNotReceiveWithAnyArgs().Publish(Arg.Any<BatchComplete>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ShouldHandleNotEnoughPaymentAmount()
        {
            var batchSummary = _container.Fake<BatchSummary>(x => x.With(y => y.Status, BatchStatus.BatchInitiated));
            batchSummary.ActualPaymentCount = batchSummary.TotalPaymentCount;
            batchSummary.ActualPaymentAmount = batchSummary.TotalPaymentAmount - 1;

            var executor = _container.Get<CheckBatchCompleteExecutor>();
            await executor.Execute(batchSummary, CancellationToken.None);

            var mediator = _container.Get<IMediator>();
            await mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<IRequest>(), Arg.Any<CancellationToken>());

            var publishEndpoint = _container.Get<IPublishEndpoint>();
            await publishEndpoint.DidNotReceiveWithAnyArgs().Publish(Arg.Any<BatchComplete>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ShouldHandleNoHeaderCount()
        {
            var batchSummary = _container.Fake<BatchSummary>(x => x.With(y => y.Status, BatchStatus.BatchInitiated));
            batchSummary.ActualPaymentCount = batchSummary.TotalPaymentCount;
            batchSummary.ActualPaymentAmount = batchSummary.TotalPaymentAmount;
            batchSummary.HeaderCount = 0;

            var executor = _container.Get<CheckBatchCompleteExecutor>();
            await executor.Execute(batchSummary, CancellationToken.None);

            var mediator = _container.Get<IMediator>();
            await mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<IRequest>(), Arg.Any<CancellationToken>());

            var publishEndpoint = _container.Get<IPublishEndpoint>();
            await publishEndpoint.DidNotReceiveWithAnyArgs().Publish(Arg.Any<BatchComplete>(), Arg.Any<CancellationToken>());
        }

        public static IEnumerable<object[]> InvalidBatchStatuses =>
            new List<object[]>
            {
                new object[] { BatchStatus.BatchCancelled },
                new object[] { BatchStatus.BatchComplete },
                new object[] { BatchStatus.BatchReceived },
            };

        [Theory]
        [MemberData(nameof(InvalidBatchStatuses))]
        public async Task ShouldHandleBatchInWrongStatusByIgnoringAndNotThrowingError(BatchStatus status)
        {
            // Arrange
            var batchSummary = _container.Fake<BatchSummary>(x => x.With(y => y.Status, status));
            batchSummary.ActualPaymentCount = batchSummary.TotalPaymentCount;
            batchSummary.ActualPaymentAmount = batchSummary.TotalPaymentAmount;
            batchSummary.HeaderCount = 1;

            var executor = _container.Get<CheckBatchCompleteExecutor>();

            // Act
            Func<Task> act = async () => await executor.Execute(batchSummary, CancellationToken.None);


            // Assert
            await act.Should().NotThrowAsync<BadRequestException>();
            var mediator = _container.Get<IMediator>();
            await mediator.DidNotReceiveWithAnyArgs()
                .Send(
                    Arg.Is<InsertBatchStatus.Command>(x =>
                        x.InboundBatchId == batchSummary.InboundBatchId && x.Status == BatchStatus.BatchReceived.Name),
                    Arg.Any<CancellationToken>());

            var publishEndpoint = _container.Get<IPublishEndpoint>();
            await publishEndpoint.DidNotReceiveWithAnyArgs().Publish(
                Arg.Is<BatchComplete>(x =>
                    x.InboundBatchId == batchSummary.InboundBatchId &&
                    x.OptumBatchId == batchSummary.OptumBatchId
                ), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ShouldHandleCompletingBatch()
        {
            var batchSummary = _container.Fake<BatchSummary>(x => x.With(y => y.Status, BatchStatus.BatchInitiated));
            batchSummary.ActualPaymentCount = batchSummary.TotalPaymentCount;
            batchSummary.ActualPaymentAmount = batchSummary.TotalPaymentAmount;
            batchSummary.RequiredPdfs = batchSummary.FoundPdfs;
            batchSummary.HeaderCount = 1;

            _container.Mock<IMediator>()
                .Send(Arg.Any<InsertBatchStatus.Command>(), Arg.Any<CancellationToken>())
                .Returns(1);

            var executor = _container.Get<CheckBatchCompleteExecutor>();
            await executor.Execute(batchSummary, CancellationToken.None);

            var mediator = _container.Get<IMediator>();
            var rowsAffected =  await mediator.Received()
                .Send(
                    Arg.Is<InsertBatchStatus.Command>(x =>
                        x.InboundBatchId == batchSummary.InboundBatchId && x.Status == BatchStatus.BatchReceived.Name),
                    Arg.Any<CancellationToken>());

            if (rowsAffected > 0)
            {
                var publishEndpoint = _container.Get<IPublishEndpoint>();
                await publishEndpoint.Received().Publish(
                    Arg.Is<BatchComplete>(x =>
                        x.InboundBatchId == batchSummary.InboundBatchId &&
                        x.OptumBatchId == batchSummary.OptumBatchId
                    ), Arg.Any<CancellationToken>());
            }
        }
    }
}
