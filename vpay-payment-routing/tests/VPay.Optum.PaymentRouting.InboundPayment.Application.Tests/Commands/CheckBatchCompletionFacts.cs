using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;
using VPay.Optum.PaymentRouting.InboundPayment.Application.Commands;
using VPay.Optum.PaymentRouting.InboundPayment.Application.Executors;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.InboundPayment.Application.Tests.Commands
{
    public class CheckBatchCompletionFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public CheckBatchCompletionFacts()
        {
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<CheckBatchCompletion.Handler>()
                    .AddMock<ILogger<CheckBatchCompletion.Handler>>()
                    .AddMock<IMediator>()
                    .AddMock<ICheckBatchCompleteExecutor>();
            });
        }

        [Fact]
        public async Task ShouldCheckBatchCompletion()
        {
            var command = _container.Fake<CheckBatchCompletion.Command>();

            var batchSummary = _container.Fake<BatchSummary>(x => x.With(y => y.Status, BatchStatus.BatchInitiated));
            var mediator = _container.Get<IMediator>();

            mediator.Send(Arg.Is<GetBatchSummarySql.Query>(x => x.InboundBatchId == command.InboundBatchId),
                Arg.Any<CancellationToken>()).Returns(batchSummary);

            var handler = _container.Get<CheckBatchCompletion.Handler>();
            await handler.Handle(command, CancellationToken.None);

            await mediator.Received()
                .Send(Arg.Is<GetBatchSummarySql.Query>(x => x.InboundBatchId == command.InboundBatchId),
                    Arg.Any<CancellationToken>());

            var executor = _container.Get<ICheckBatchCompleteExecutor>();
            await executor.Received().Execute(Arg.Is<BatchSummary>(x => x == batchSummary), Arg.Any<CancellationToken>());
        }
    }
}
