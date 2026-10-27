using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Optum.PaymentRouting.Application.InboundPayments.Queries;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.BatchManifest;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Application.Tests.InboundPayments.Queries
{
    public class GetBatchWithPaymentsFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public GetBatchWithPaymentsFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<GetBatchWithPayments.Handler>()
                    .AddMock<IMediator>()
                    .AddMock<ILogger<GetBatchWithPayments.Handler>>();
            });

        [Fact]
        public async Task Should_ReturnPaymentRecord_When_ValidInputIsGiven()
        {
            var batch = _container.Fake<BatchManifest>(x => x.With(bm => bm.Status, BatchStatus.BatchInitiated));
            var mediator = _container.Get<IMediator>();
            mediator.Send(Arg.Any<GetBatchManifestSql.Query>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(batch);
            var command = new GetBatchWithPayments.Query {Id = batch.InboundBatchId};
            var handler = _container.Get<GetBatchWithPayments.Handler>();
            
            var result = await handler.Handle(command, CancellationToken.None);

            result.InboundBatchId.Should().Be(batch.InboundBatchId);
            result.BatchCreatedDateTime.Should().Be(batch.BatchCreatedDateTime);
            result.HeaderCreatedDateTime.Should().Be(batch.HeaderCreatedDateTime);
            result.InboundHeaderId.Should().Be(batch.InboundHeaderId);
            result.OptumBatchId.Should().Be(batch.OptumBatchId);
            result.Payer835Id.Should().Be(batch.Payer835Id);
            result.HeaderData.Should().Be(batch.HeaderData);
            result.TotalPaymentCount.Should().Be(batch.TotalPaymentCount);
            result.TotalPaymentAmount.Should().Be(batch.TotalPaymentAmount);
        }

        [Fact]
        public async Task Should_ReturnNull_When_NullReturned()
        {
            var batch = _container.Fake<BatchManifest>(x => x.With(bm => bm.Status, BatchStatus.BatchInitiated));
            var mediator = _container.Get<IMediator>();
            mediator.Send(Arg.Any<GetBatchManifestSql.Query>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs((BatchManifest)null);
            var command = new GetBatchWithPayments.Query { Id = batch.InboundBatchId };
            var handler = _container.Get<GetBatchWithPayments.Handler>();

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().BeNull();
        }
    }
}
