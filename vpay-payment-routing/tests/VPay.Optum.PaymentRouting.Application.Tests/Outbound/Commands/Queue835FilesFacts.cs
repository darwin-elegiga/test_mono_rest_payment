using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Optum.PaymentRouting.Application.Outbound.Commands;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Commands;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Application.Tests.Outbound.Commands
{
    public class Queue835FilesFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public Queue835FilesFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<Queue835Files.Handler>()
                    .AddMock<ILogger<Queue835Files.Handler>>()
                    .AddMock<IMediator>()
                    .AddMock<IPublishEndpoint>();
            });

        [Fact]
        public async Task ShouldHandleNoPaymentsInStatus()
        {
            var expectedResult = 0;
            var mediator = _container.Get<IMediator>();

            var expectedPayments = Array.Empty<GetInboundPaymentByStatus.Result>();
            mediator.Send(Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.Out835Built),
                Arg.Any<CancellationToken>()).Returns(expectedPayments);

            var handler = _container.Get<Queue835Files.Handler>();
            var result = await handler.Handle(new Queue835Files.Command(), CancellationToken.None);

            result.Should().Be(expectedResult);

            await mediator.Received().Send(
                Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.Out835Built),
                Arg.Any<CancellationToken>());

            await mediator.DidNotReceive().Send(Arg.Any<InsertOutbound835File.Command>(), Arg.Any<CancellationToken>());

            var publishEndpoint = _container.Get<IPublishEndpoint>();
            await publishEndpoint.DidNotReceiveWithAnyArgs()
                .Publish(Arg.Any<Outbound835FileMessage>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ShouldHandleSinglePayment()
        {
            var expectedResult = 1;
            var expectedBatchId = _container.Fake<long>();
            var payment = _container.Fake<GetInboundPaymentByStatus.Result>(x => x.With(y => y.Status, PaymentStatus.Out835Built));
            var expectedPayments = new List<GetInboundPaymentByStatus.Result> { payment };

            var mediator = _container.Get<IMediator>();
            mediator.Send(Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.Out835Built),
                Arg.Any<CancellationToken>()).Returns(expectedPayments);

            mediator.Send(
                Arg.Is<InsertOutbound835File.Command>(x =>
                    x.PayerAlias == payment.PayerAlias && x.OutboundRefEo == payment.OutboundRefEo),
                Arg.Any<CancellationToken>()).Returns(expectedBatchId);

            var handler = _container.Get<Queue835Files.Handler>();
            var result = await handler.Handle(new Queue835Files.Command(), CancellationToken.None);

            result.Should().Be(expectedResult);

            await mediator.Received().Send(
                Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.Out835Built),
                Arg.Any<CancellationToken>());

            await mediator.Received().Send(
                Arg.Is<InsertOutbound835File.Command>(x =>
                    x.PayerAlias == payment.PayerAlias && x.OutboundRefEo == payment.OutboundRefEo),
                Arg.Any<CancellationToken>());

            var publishEndpoint = _container.Get<IPublishEndpoint>();
            await publishEndpoint.Received()
                .Publish(Arg.Is<Outbound835FileMessage>(x => x.Id == expectedBatchId), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ShouldHandleGroupedPayments()
        {
            var expectedResult = 1;
            var expectedBatchId = _container.Fake<long>();
            var payerAlias = _container.Fake<string>();
            var outboundRefEo = _container.Fake<string>();
            var payments = _container.FakeMany<GetInboundPaymentByStatus.Result>(x => x.With(y => y.Status, PaymentStatus.Out835Built).With(y => y.PayerAlias, payerAlias).With(y => y.OutboundRefEo, outboundRefEo));
            var expectedPayments = payments.ToList();

            var mediator = _container.Get<IMediator>();
            mediator.Send(Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.Out835Built),
                Arg.Any<CancellationToken>()).Returns(expectedPayments);

            mediator.Send(
                Arg.Is<InsertOutbound835File.Command>(x =>
                    x.PayerAlias == payerAlias && x.OutboundRefEo == outboundRefEo),
                Arg.Any<CancellationToken>()).Returns(expectedBatchId);

            var handler = _container.Get<Queue835Files.Handler>();
            var result = await handler.Handle(new Queue835Files.Command(), CancellationToken.None);

            result.Should().Be(expectedResult);

            await mediator.Received().Send(
                Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.Out835Built),
                Arg.Any<CancellationToken>());

            await mediator.Received().Send(
                Arg.Is<InsertOutbound835File.Command>(x =>
                    x.PayerAlias == payerAlias && x.OutboundRefEo == outboundRefEo),
                Arg.Any<CancellationToken>());

            await mediator.ReceivedWithAnyArgs(1)
                .Send(Arg.Any<InsertOutbound835File.Command>(), Arg.Any<CancellationToken>());

            var publishEndpoint = _container.Get<IPublishEndpoint>();
            await publishEndpoint.Received()
                .Publish(Arg.Is<Outbound835FileMessage>(x => x.Id == expectedBatchId), Arg.Any<CancellationToken>());

            await publishEndpoint.ReceivedWithAnyArgs(1)
                .Publish(Arg.Any<Outbound835FileMessage>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ShouldHandlePaymentsDifferentOutboundRefEo()
        {
            var expectedResult = 2;
            var expectedBatchId1 = _container.Fake<long>();
            var expectedBatchId2 = _container.Fake<long>();
            var payerAlias = _container.Fake<string>();
            var payment1 = _container.Fake<GetInboundPaymentByStatus.Result>(x =>
                x.With(y => y.Status, PaymentStatus.Out835Built).With(y => y.PayerAlias, payerAlias));

            var payment2 = _container.Fake<GetInboundPaymentByStatus.Result>(x =>
                x.With(y => y.Status, PaymentStatus.Out835Built).With(y => y.PayerAlias, payerAlias));
            var expectedPayments = new List<GetInboundPaymentByStatus.Result> { payment1, payment2 };

            var mediator = _container.Get<IMediator>();
            mediator.Send(Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.Out835Built),
                Arg.Any<CancellationToken>()).Returns(expectedPayments);

            mediator.Send(
                Arg.Is<InsertOutbound835File.Command>(x =>
                    x.PayerAlias == payerAlias && x.OutboundRefEo == payment1.OutboundRefEo),
                Arg.Any<CancellationToken>()).Returns(expectedBatchId1);

            mediator.Send(
                Arg.Is<InsertOutbound835File.Command>(x =>
                    x.PayerAlias == payerAlias && x.OutboundRefEo == payment2.OutboundRefEo),
                Arg.Any<CancellationToken>()).Returns(expectedBatchId2);

            var handler = _container.Get<Queue835Files.Handler>();
            var result = await handler.Handle(new Queue835Files.Command(), CancellationToken.None);

            result.Should().Be(expectedResult);

            await mediator.Received().Send(
                Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.Out835Built),
                Arg.Any<CancellationToken>());

            await mediator.Received().Send(
                Arg.Is<InsertOutbound835File.Command>(x =>
                    x.PayerAlias == payerAlias && x.OutboundRefEo == payment1.OutboundRefEo),
                Arg.Any<CancellationToken>());

            await mediator.Received().Send(
                Arg.Is<InsertOutbound835File.Command>(x =>
                    x.PayerAlias == payerAlias && x.OutboundRefEo == payment2.OutboundRefEo),
                Arg.Any<CancellationToken>());

            await mediator.ReceivedWithAnyArgs(2)
                .Send(Arg.Any<InsertOutbound835File.Command>(), Arg.Any<CancellationToken>());

            var publishEndpoint = _container.Get<IPublishEndpoint>();
            await publishEndpoint.Received()
                .Publish(Arg.Is<Outbound835FileMessage>(x => x.Id == expectedBatchId1), Arg.Any<CancellationToken>());

            await publishEndpoint.Received()
                .Publish(Arg.Is<Outbound835FileMessage>(x => x.Id == expectedBatchId2), Arg.Any<CancellationToken>());

            await publishEndpoint.ReceivedWithAnyArgs(2)
                .Publish(Arg.Any<Outbound835FileMessage>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ShouldHandlePaymentsDifferentPayerAlias()
        {
            var expectedResult = 2;
            var expectedBatchId1 = _container.Fake<long>();
            var expectedBatchId2 = _container.Fake<long>();
            var outboundRefEo = _container.Fake<string>();
            var payment1 = _container.Fake<GetInboundPaymentByStatus.Result>(x =>
                x.With(y => y.Status, PaymentStatus.Out835Built).With(y => y.OutboundRefEo, outboundRefEo));

            var payment2 = _container.Fake<GetInboundPaymentByStatus.Result>(x =>
                x.With(y => y.Status, PaymentStatus.Out835Built).With(y => y.OutboundRefEo, outboundRefEo));
            var expectedPayments = new List<GetInboundPaymentByStatus.Result> { payment1, payment2 };

            var mediator = _container.Get<IMediator>();
            mediator.Send(Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.Out835Built),
                Arg.Any<CancellationToken>()).Returns(expectedPayments);

            mediator.Send(
                Arg.Is<InsertOutbound835File.Command>(x =>
                    x.PayerAlias == payment1.PayerAlias && x.OutboundRefEo == outboundRefEo),
                Arg.Any<CancellationToken>()).Returns(expectedBatchId1);

            mediator.Send(
                Arg.Is<InsertOutbound835File.Command>(x =>
                    x.PayerAlias == payment2.PayerAlias && x.OutboundRefEo == outboundRefEo),
                Arg.Any<CancellationToken>()).Returns(expectedBatchId2);

            var handler = _container.Get<Queue835Files.Handler>();
            var result = await handler.Handle(new Queue835Files.Command(), CancellationToken.None);

            result.Should().Be(expectedResult);

            await mediator.Received().Send(
                Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.Out835Built),
                Arg.Any<CancellationToken>());

            await mediator.Received().Send(
                Arg.Is<InsertOutbound835File.Command>(x =>
                    x.PayerAlias == payment1.PayerAlias && x.OutboundRefEo == outboundRefEo),
                Arg.Any<CancellationToken>());

            await mediator.Received().Send(
                Arg.Is<InsertOutbound835File.Command>(x =>
                    x.PayerAlias == payment2.PayerAlias && x.OutboundRefEo == outboundRefEo),
                Arg.Any<CancellationToken>());

            await mediator.ReceivedWithAnyArgs(2)
                .Send(Arg.Any<InsertOutbound835File.Command>(), Arg.Any<CancellationToken>());

            var publishEndpoint = _container.Get<IPublishEndpoint>();
            await publishEndpoint.Received()
                .Publish(Arg.Is<Outbound835FileMessage>(x => x.Id == expectedBatchId1), Arg.Any<CancellationToken>());

            await publishEndpoint.Received()
                .Publish(Arg.Is<Outbound835FileMessage>(x => x.Id == expectedBatchId2), Arg.Any<CancellationToken>());

            await publishEndpoint.ReceivedWithAnyArgs(2)
                .Publish(Arg.Any<Outbound835FileMessage>(), Arg.Any<CancellationToken>());
        }
    }
}
