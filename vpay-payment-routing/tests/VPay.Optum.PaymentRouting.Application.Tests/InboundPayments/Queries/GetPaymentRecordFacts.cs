using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Optum.PaymentRouting.Application.InboundPayments.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Application.Tests.InboundPayments.Queries
{
    public class GetPaymentRecordFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public GetPaymentRecordFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<GetPaymentRecord.Handler>()
                    .AddMock<IMediator>()
                    .AddMock<ILogger<GetPaymentRecord.Handler>>();
            });

        [Fact]
        public async Task Should_ReturnPaymentRecord_When_ValidInputIsGiven()
        {
            var paymentRecord = _container.Fake<GetInboundPaymentWithClobByIdSql.Result>();
            var mediator = _container.Get<IMediator>();
            mediator.Send(Arg.Any<GetInboundPaymentWithClobByIdSql.Query>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(paymentRecord);
            var command = new GetPaymentRecord.Query {Id = paymentRecord.Id};
            var handler = _container.Get<GetPaymentRecord.Handler>();
            
            var result = await handler.Handle(command, CancellationToken.None);

            result.PaymentClob.Should().Be(paymentRecord.PaymentClob);
            result.InboundBatchId.Should().Be(paymentRecord.InboundBatchId);
            result.Payer835Id.Should().Be(paymentRecord.Payer835Id);
            result.FileProcId.Should().Be(paymentRecord.FileProcId);
            result.PayerAlias.Should().Be(paymentRecord.PayerAlias);
            result.CreateDateTime.Should().Be(paymentRecord.CreateDateTime);
            result.UserKey.Should().Be(paymentRecord.UserKey);
        }

        [Fact]
        public async Task Should_ReturnNull_When_NullIsReturned()
        {
            var paymentRecord = _container.Fake<GetInboundPaymentWithClobByIdSql.Result>();
            var mediator = _container.Get<IMediator>();
            mediator.Send(Arg.Any<GetInboundPaymentWithClobByIdSql.Query>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs((GetInboundPaymentWithClobByIdSql.Result)null);
            var command = new GetPaymentRecord.Query { Id = paymentRecord.Id };
            var handler = _container.Get<GetPaymentRecord.Handler>();

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().BeNull();
        }
    }
}
