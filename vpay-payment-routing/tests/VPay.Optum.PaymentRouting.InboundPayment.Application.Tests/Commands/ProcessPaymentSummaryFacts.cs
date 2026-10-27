using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Extensions.Exceptions;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;
using VPay.Optum.PaymentRouting.Data.Sql.InboundBatch.Queries;
using VPay.Optum.PaymentRouting.InboundPayment.Application.Commands;
using VPay.Optum.PaymentRouting.InboundPayment.Application.Executors;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Application.Tests.InboundPayments.Commands
{
    public class ProcessPaymentSummaryFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public ProcessPaymentSummaryFacts()
        {
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<ProcessPaymentSummary.Handler>()
                    .AddMock<ILogger<ProcessPaymentSummary.Handler>>()
                    .AddMock<IMediator>()
                    .AddMock<ICheckBatchCompleteExecutor>();
            });
        }

        [Fact]
        public async Task ShouldThrowIfTotalPaymentCountNotANumber()
        {
            var expectedMessage = "Batch Total Payment Count invalid";
            var paymentSummary = _container.Fake<PaymentSummary>(x => x.With(y => y.TotalPaymentCount, "abc"));
            var command =
                _container.Fake<ProcessPaymentSummary.Command>(x => x.With(y => y.PaymentSummary, paymentSummary));

            var handler = _container.Get<ProcessPaymentSummary.Handler>();
            Func<Task> act = () => handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<VPayAlertableException>().WithMessage(expectedMessage);
        }

        [Fact]
        public async Task ShouldThrowIfTotalPaymentCountIsNegative()
        {
            var expectedMessage = "Batch Total Payment Count invalid";
            var paymentSummary = _container.Fake<PaymentSummary>(x => x.With(y => y.TotalPaymentCount, "-1"));
            var command =
                _container.Fake<ProcessPaymentSummary.Command>(x => x.With(y => y.PaymentSummary, paymentSummary));

            var handler = _container.Get<ProcessPaymentSummary.Handler>();
            Func<Task> act = () => handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<VPayAlertableException>().WithMessage(expectedMessage);
        }

        [Fact]
        public async Task ShouldThrowIfTotalPaymentAmountNotANumber()
        {
            var expectedMessage = "Batch Total Payment Amount invalid";
            var paymentSummary = _container.Fake<PaymentSummary>(x =>
                x.With(y => y.TotalPaymentCount, new Random().Next().ToString()).With(y => y.TotalPaymentAmount, "abc"));
            var command =
                _container.Fake<ProcessPaymentSummary.Command>(x => x.With(y => y.PaymentSummary, paymentSummary));

            var handler = _container.Get<ProcessPaymentSummary.Handler>();
            Func<Task> act = () => handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<VPayAlertableException>().WithMessage(expectedMessage);
        }

        [Fact]
        public async Task ShouldThrowIfTotalPaymentAmountIsNegative()
        {
            var expectedMessage = "Batch Total Payment Amount invalid";
            var paymentSummary = _container.Fake<PaymentSummary>(x =>
                x.With(y => y.TotalPaymentCount, new Random().Next().ToString())
                    .With(y => y.TotalPaymentAmount, "-1"));
            var command =
                _container.Fake<ProcessPaymentSummary.Command>(x => x.With(y => y.PaymentSummary, paymentSummary));

            var handler = _container.Get<ProcessPaymentSummary.Handler>();
            Func<Task> act = () => handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<VPayAlertableException>().WithMessage(expectedMessage);
        }

        [Fact]
        public async Task ShouldThrowIfTotalPaymentAmountAlreadyExistsAndIsDifferent()
        {
            var command = _container.Fake<ProcessPaymentSummary.Command>();
            var existingBatch = _container.Fake<GetInboundBatchByOptumBatchIdSql.Result>(x => x.With(b => b.Status, BatchStatus.BatchInitiated));
            var mediatr = _container.Get<IMediator>();
            mediatr.Send(Arg.Any<GetInboundBatchByOptumBatchIdSql.Query>()).ReturnsForAnyArgs(existingBatch);

            var handler = _container.Get<ProcessPaymentSummary.Handler>();
            Func<Task> act = () => handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<VPayAlertableException>();
            await mediatr.DidNotReceive().Send(Arg.Any<InsertInboundBatchSql.Query>());
        }

        [Fact]
        public async Task ShouldNotThrowIfTotalPaymentAmountAlreadyExistsAndIsSame()
        {
            var totalPaymentAmount = _container.Fake<int>();
            var totalPaymentCount = _container.Fake<int>();
            var paymentSummary = _container.Fake<PaymentSummary>(x =>
                x.With(y => y.TotalPaymentCount, totalPaymentCount.ToString())
                    .With(y => y.TotalPaymentAmount, totalPaymentAmount.ToString()));

            var command = _container.Fake<ProcessPaymentSummary.Command>(x =>
                x.With(s => s.PaymentSummary, paymentSummary));
            var existingBatch = _container.Fake<GetInboundBatchByOptumBatchIdSql.Result>(x =>
                x.With(b => b.TotalPaymentAmount, totalPaymentAmount)
                    .With(b => b.TotalPaymentCount, totalPaymentCount)
                    .With(b => b.Status, BatchStatus.BatchInitiated));
            var mediatr = _container.Get<IMediator>();
            mediatr.Send(Arg.Any<GetInboundBatchByOptumBatchIdSql.Query>()).ReturnsForAnyArgs(existingBatch);

            var handler = _container.Get<ProcessPaymentSummary.Handler>();
            Func<Task> act = () => handler.Handle(command, CancellationToken.None);

            await act.Should().NotThrowAsync<VPayAlertableException>();
        }

        [Fact]
        public async Task ShouldSaveAndCheckBatchSummary()
        {
            var random = new Random();
            var paymentSummary = _container.Fake<PaymentSummary>(x =>
                x.With(y => y.TotalPaymentCount, random.Next().ToString())
                    .With(y => y.TotalPaymentAmount, (random.NextDouble() * 100).ToString(CultureInfo.InvariantCulture)));
            var command =
                _container.Fake<ProcessPaymentSummary.Command>(x => x.With(y => y.PaymentSummary, paymentSummary));

            var batchSummary = _container.Fake<BatchSummary>(x => x.With(y => y.Status, BatchStatus.BatchInitiated));
            var mediator = _container.Get<IMediator>();
            mediator.Send(Arg.Is<InsertInboundBatchSql.Query>(x =>
                x.OptumBatchId == paymentSummary.BatchId &&
                x.TotalPaymentCount == int.Parse(paymentSummary.TotalPaymentCount) &&
                x.TotalPaymentAmount == decimal.Parse(paymentSummary.TotalPaymentAmount) &&
                x.Payer835Id == paymentSummary.Payer835Id)).Returns(batchSummary);

            var handler = _container.Get<ProcessPaymentSummary.Handler>();
            await handler.Handle(command, CancellationToken.None);

            await mediator.Received().Send(Arg.Is<InsertInboundBatchSql.Query>(x =>
                x.OptumBatchId == paymentSummary.BatchId &&
                x.TotalPaymentCount == int.Parse(paymentSummary.TotalPaymentCount) &&
                x.TotalPaymentAmount == decimal.Parse(paymentSummary.TotalPaymentAmount) &&
                x.Payer835Id == paymentSummary.Payer835Id));

            var executor = _container.Get<ICheckBatchCompleteExecutor>();
            await executor.Received().Execute(Arg.Is<BatchSummary>(x => x == batchSummary), Arg.Any<CancellationToken>());
        }
    }
}
