using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.Optum.PaymentRouting.Application.InboundPayments.Commands;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Application.Tests.InboundPayments.Commands
{
    public class CancelInboundBatchFacts
    {
        private readonly PaymentRoutingTestingContainer _container;
        private const long BATCHID_NOTEXIST = 1;
        private const long BATCHID_COMPLETE = 2;
        private const long BATCHID_INCOMPLETE = 3;
        private static IMediator MockMediator()
        {
            var mock = new Mock<IMediator>();
            mock.Setup(m => m.Send<BatchSummary>(It.Is<GetBatchSummarySql.Query>(x => x.InboundBatchId == BATCHID_NOTEXIST), It.IsAny<CancellationToken>()))
                .ReturnsAsync((BatchSummary)null);
            mock.Setup(m => m.Send<BatchSummary>(It.Is<GetBatchSummarySql.Query>(x => x.InboundBatchId == BATCHID_COMPLETE), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BatchSummary { Status = BatchStatus.BatchComplete });
            mock.Setup(m => m.Send<BatchSummary>(It.Is<GetBatchSummarySql.Query>(x => x.InboundBatchId == BATCHID_INCOMPLETE), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BatchSummary { });
            return mock.Object;
        }

        public CancelInboundBatchFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services
                    .AddSingleton<CancelInboundBatch.Handler>()
                    .AddSingleton(MockMediator())
                    .AddMock<ILogger<CancelInboundBatch.Handler>>();
            });

        [Theory]
        [InlineData(BATCHID_INCOMPLETE)]
        public async Task Should_NotThrowError_When_ValidInputIsGiven(long batchId)
        {
            //Arrange
            var command = new CancelInboundBatch.Command {InboundBatchId = batchId };
            var handler = _container.Get<CancelInboundBatch.Handler>();

            //Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);


            //Assert
            await act.Should().NotThrowAsync();
        }

        [Theory]
        [InlineData(BATCHID_COMPLETE)]
        [InlineData(BATCHID_NOTEXIST)]
        public async Task Should_ThrowError_When_InvalidInputIsGiven(long batchId)
        {
            //Arrange
            var command = new CancelInboundBatch.Command { InboundBatchId = batchId };
            CancelInboundBatch.Handler handler = _container.Get<CancelInboundBatch.Handler>();

            //Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);


            //Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
