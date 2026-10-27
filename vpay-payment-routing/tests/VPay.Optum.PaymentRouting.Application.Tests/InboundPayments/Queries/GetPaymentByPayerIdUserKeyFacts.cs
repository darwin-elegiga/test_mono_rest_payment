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
    public class GetPaymentByPayerIdUserKeyFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public GetPaymentByPayerIdUserKeyFacts()
        {
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<GetPaymentByPayerIdUserKey.Handler>()
                    .AddMock<ILogger<GetPaymentByPayerIdUserKey.Handler>>()
                    .AddMock<IMediator>();
            });
        }

        [Fact]
        public async Task ShouldReturnNull()
        {
            var query = _container.Fake<GetPaymentByPayerIdUserKey.Query>();
            
            var mediator = _container.Get<IMediator>();
            mediator.Send(
                    Arg.Is<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Query>(x =>
                        x.Payer835Id == query.PayerId && x.UserKey == query.UserKey),
                    Arg.Any<CancellationToken>())
                .Returns((GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result)null);

            var handler = _container.Get<GetPaymentByPayerIdUserKey.Handler>();
            var result = await handler.Handle(query, CancellationToken.None);

            result.Should().BeNull();

            await mediator.Received().Send(
                Arg.Is<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Query>(x => x.Payer835Id == query.PayerId && x.UserKey == query.UserKey),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ShouldReturnInboundPayment()
        {
            var query = _container.Fake<GetPaymentByPayerIdUserKey.Query>();

            var inboundPayment = _container.Fake<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result>();
            var mediator = _container.Get<IMediator>();
            mediator.Send(
                    Arg.Is<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Query>(x => x.Payer835Id == query.PayerId && x.UserKey == query.UserKey),
                    Arg.Any<CancellationToken>())
                .Returns(inboundPayment);

            var handler = _container.Get<GetPaymentByPayerIdUserKey.Handler>();
            var result = await handler.Handle(query, CancellationToken.None);

            result.Should().BeEquivalentTo(inboundPayment);

            await mediator.Received().Send(
                Arg.Is<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Query>(x => x.Payer835Id == query.PayerId && x.UserKey == query.UserKey),
                Arg.Any<CancellationToken>());
        }
    }
}
