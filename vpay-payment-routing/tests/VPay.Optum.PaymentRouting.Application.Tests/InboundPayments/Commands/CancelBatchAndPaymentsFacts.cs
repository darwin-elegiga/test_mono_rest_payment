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
using VPay.Optum.PaymentRouting.Data.Sql.InboundBatch.Commands;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Application.Tests.InboundPayments.Commands
{
    public class CancelBatchAndPaymentsFacts
    {
        private readonly PaymentRoutingTestingContainer _container;
        private const long BATCHID_NOTEXIST = 1;
        private const long BATCHID_COMPLETE = 2;
        private const long BATCHID_INCOMPLETE = 3;

        private static IMediator MockMediator()
        {
            var mock = new Mock<IMediator>();
            mock.Setup(m => m.Send(It.Is<GetBatchSummarySql.Query>(x => x.InboundBatchId == BATCHID_NOTEXIST), It.IsAny<CancellationToken>()))
                .ReturnsAsync((BatchSummary)null);
            mock.Setup(m => m.Send(It.Is<GetBatchSummarySql.Query>(x => x.InboundBatchId == BATCHID_COMPLETE), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BatchSummary { Status = BatchStatus.BatchComplete });
            mock.Setup(m => m.Send(It.Is<GetBatchSummarySql.Query>(x => x.InboundBatchId == BATCHID_INCOMPLETE), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BatchSummary());
            mock.Setup(m => m.Send(It.IsAny<CancelBatchAndPaymentsSql.Command>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Unit());
            return mock.Object;
        }

        public CancelBatchAndPaymentsFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services
                    .AddSingleton<CancelBatchAndPayments.Handler>()
                    .AddSingleton(MockMediator())
                    .AddMock<ILogger<CancelBatchAndPayments.Handler>>();
            });

        [Theory]
        [InlineData(BATCHID_INCOMPLETE)]
        public async Task Should_NotThrowError_When_ValidInputIsGiven(long batchId)
        {
            var command = new CancelBatchAndPayments.Command(batchId, "PROD-12345");
            var handler = _container.Get<CancelBatchAndPayments.Handler>();

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().NotThrowAsync();
        }

        [Theory]
        [InlineData(BATCHID_COMPLETE)]
        [InlineData(BATCHID_NOTEXIST)]
        public async Task Should_ThrowError_When_InvalidInputIsGiven(long batchId)
        {
            var command = new CancelBatchAndPayments.Command(batchId, "PROD-12345");
            CancelBatchAndPayments.Handler handler = _container.Get<CancelBatchAndPayments.Handler>();

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
