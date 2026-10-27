using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.Optum.PaymentRouting.Application.InboundPayments.Commands;
using VPay.Optum.PaymentRouting.Common.Helpers;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Application.Tests.InboundPayments.Commands
{
    public class CancelInboundPaymentFacts
    {
        private readonly PaymentRoutingTestingContainer _container;
        private const long PAYMENTID_NOTEXIST = 1;
        private const long PAYMENTID_NOBATCH = 2;
        private const long PAYMENTID_BATCHCOMPLETE = 3;
        private const long PAYMENTID_BATCHINCOMPLETE = 4;
        private const long BATCHID_NOTEXIST = 1;
        private const long BATCHID_COMPLETE = 2;
        private const long BATCHID_INCOMPLETE = 3;
        private static IMediator MockMediator()
        {
            var mock = new Mock<IMediator>();
            mock.Setup(m => m.Send<GetInboundPaymentWithClobByIdSql.Result>(
                    It.Is<GetInboundPaymentWithClobByIdSql.Query>(x => x.InboundPaymentId == PAYMENTID_NOTEXIST),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((GetInboundPaymentWithClobByIdSql.Result)null);
            mock.Setup(m => m.Send<GetInboundPaymentWithClobByIdSql.Result>(
                    It.Is<GetInboundPaymentWithClobByIdSql.Query>(x => x.InboundPaymentId == PAYMENTID_NOBATCH),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetInboundPaymentWithClobByIdSql.Result { InboundBatchId = BATCHID_NOTEXIST });
            mock.Setup(m => m.Send<GetInboundPaymentWithClobByIdSql.Result>(
                    It.Is<GetInboundPaymentWithClobByIdSql.Query>(x => x.InboundPaymentId == PAYMENTID_BATCHCOMPLETE),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetInboundPaymentWithClobByIdSql.Result { InboundBatchId = BATCHID_COMPLETE });
            mock.Setup(m => m.Send<GetInboundPaymentWithClobByIdSql.Result>(
                    It.Is<GetInboundPaymentWithClobByIdSql.Query>(x => x.InboundPaymentId == PAYMENTID_BATCHINCOMPLETE),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetInboundPaymentWithClobByIdSql.Result { InboundBatchId = BATCHID_INCOMPLETE });
            mock.Setup(m => m.Send<BatchSummary>(
                    It.Is<GetBatchSummarySql.Query>(x => x.InboundBatchId == BATCHID_NOTEXIST),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((BatchSummary)null);
            mock.Setup(m => m.Send<BatchSummary>(
                    It.Is<GetBatchSummarySql.Query>(x => x.InboundBatchId == BATCHID_COMPLETE),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BatchSummary { Status = BatchStatus.BatchComplete });
            mock.Setup(m => m.Send<BatchSummary>(
                    It.Is<GetBatchSummarySql.Query>(x => x.InboundBatchId == BATCHID_INCOMPLETE),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BatchSummary { });
            return mock.Object;
        }

        public CancelInboundPaymentFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            
                services
                    .AddMock<ILogger<CancelInboundPayment.Handler>>()
                    .AddSingleton(MockMediator())
                    .AddMock<IUserInfo>()
                    .AddSingleton<CancelInboundPayment.Handler>()
            );

        [Theory]
        [InlineData(PAYMENTID_BATCHINCOMPLETE)]
        public async Task Handle_ShouldNotThrow(long paymentId)
        {
            // arrange
            var command = new CancelInboundPayment.Command(paymentId);
            var handler = _container.Get<CancelInboundPayment.Handler>();

            // act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // assert
            await act.Should().NotThrowAsync();
        }

        [Theory]
        [InlineData(PAYMENTID_NOBATCH)]
        [InlineData(PAYMENTID_NOTEXIST)]
        [InlineData(PAYMENTID_BATCHCOMPLETE)]
        public async Task Handle_ShouldThrow(long paymentId)
        {
            // arrange
            var command = new CancelInboundPayment.Command(paymentId);
            var handler = _container.Get<CancelInboundPayment.Handler>();

            // act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
