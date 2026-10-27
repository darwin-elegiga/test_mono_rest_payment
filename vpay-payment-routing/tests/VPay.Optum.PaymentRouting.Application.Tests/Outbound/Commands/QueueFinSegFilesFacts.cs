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
using VPay.Optum.PaymentRouting.Data.Sql.Models;
using VPay.Optum.PaymentRouting.Data.Sql.OutboundFinSeg.Commands;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Application.Tests.Outbound.Commands
{
    public class QueueFinSegFilesFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public QueueFinSegFilesFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<QueueFinSegFiles.Handler>()
                    .AddMock<ILogger<QueueFinSegFiles.Handler>>()
                    .AddMock<IMediator>()
                    .AddMock<IPublishEndpoint>();
            });

        [Fact]
        public async Task ShouldHandleNoPaymentsInStatus()
        {
            //Arrange
            var expectedResult = 0;
            var mediator = _container.Get<IMediator>();
            var expectedPayments = Array.Empty<GetInboundPaymentByStatus.Result>();

            mediator.Send(Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.FinSegBuilt),
                Arg.Any<CancellationToken>()).Returns(expectedPayments);

            //Act
            var handler = _container.Get<QueueFinSegFiles.Handler>();

            var result = await handler.Handle(new QueueFinSegFiles.Command(), CancellationToken.None);


            //Assert


            result.Should().Be(expectedResult);

            await mediator.Received().Send(
                Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.FinSegBuilt),
                Arg.Any<CancellationToken>());

            await mediator.DidNotReceive().Send(Arg.Any<InsertOutboundFinSegFiles.Command>(), Arg.Any<CancellationToken>());

            var publishEndpoint = _container.Get<IPublishEndpoint>();
            await publishEndpoint.DidNotReceiveWithAnyArgs()
                .Publish(Arg.Any<OutboundFinSegFileMessage>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ShouldHandleSinglePayment()
        {
            //Arrange
            var expectedResult = 1;
            var mediator = _container.Get<IMediator>();
            var payerAlias1 = _container.Fake<string>();
            var outboundRefEo1 = _container.Fake<string>();

            var payments = _container.FakeMany<GetInboundPaymentByStatus.Result>
            (x => x
            .With(y => y.Status, PaymentStatus.FinSegBuilt)
            .With(y => y.PayerAlias, payerAlias1)
            .With(y => y.OutboundRefEo, outboundRefEo1), 1);
            var expectedPayments = payments.ToList();

            mediator
                .Send(Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.FinSegBuilt),
                    Arg.Any<CancellationToken>())
                .Returns(expectedPayments);

            var outboundFinSegSequence = _container.Fake<OutboundFinSegSequence>();
            mediator
                .Send(Arg.Any<GetNextFinSegSequence.Command>(),
                    Arg.Any<CancellationToken>())
                .Returns(outboundFinSegSequence);

            var finSegIds = _container.Fake<InsertOutboundFinSegFiles.Result>();
            mediator
                .Send(Arg.Any<InsertOutboundFinSegFiles.Command>(),
                    Arg.Any<CancellationToken>())
                .Returns(finSegIds);
            var outboundFinSegFileMessage = new OutboundFinSegFileMessage
            {
                OutboundFinSeg835FileId = finSegIds.OutboundFinSeg835FileId,
                OutboundFinSegTxtFileId = finSegIds.OutboundFinSegTxtFileId
            };

            var publishEndpoint = _container.Get<IPublishEndpoint>();
            await publishEndpoint.Publish(Arg.Is<OutboundFinSegFileMessage>(_ =>
           _.OutboundFinSeg835FileId == outboundFinSegFileMessage.OutboundFinSeg835FileId &&
           _.OutboundFinSegTxtFileId == outboundFinSegFileMessage.OutboundFinSegTxtFileId
            ));


            //Act
            var handler = _container.Get<QueueFinSegFiles.Handler>();

            var result = await handler.Handle(new QueueFinSegFiles.Command(), CancellationToken.None);


            //Assert - Should call the correct things
            result.Should().Be(expectedResult);

            await mediator.Received().Send(
                Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.FinSegBuilt),
                Arg.Any<CancellationToken>());

            result.Should().Be(expectedResult);

            await mediator
                .Received()
                .Send(Arg.Any<GetNextFinSegSequence.Command>(), Arg.Any<CancellationToken>());

            await mediator
                .Received()
                .Send(Arg.Any<InsertOutboundFinSegFiles.Command>(), Arg.Any<CancellationToken>());

            await publishEndpoint
                .Received()
                .Publish(Arg.Any<OutboundFinSegFileMessage>());
        }

        [Fact]
        public async Task ShouldHandleGroupedPayments()
        {
            //Arrange
            var expectedResult = 1;
            var mediator = _container.Get<IMediator>();
            var payerAlias1 = _container.Fake<string>();
            var outboundRefEo1 = _container.Fake<string>();

            var payments = _container.FakeMany<GetInboundPaymentByStatus.Result>
            (x => x
            .With(y => y.Status, PaymentStatus.FinSegBuilt)
            .With(y => y.PayerAlias, payerAlias1)
            .With(y => y.OutboundRefEo, outboundRefEo1), 6);
            var expectedPayments = payments.ToList();

            mediator
                .Send(Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.FinSegBuilt),
                    Arg.Any<CancellationToken>())
                .Returns(expectedPayments);

            var outboundFinSegSequence = _container.Fake<OutboundFinSegSequence>();
            mediator
                .Send(Arg.Any<GetNextFinSegSequence.Command>(),
                    Arg.Any<CancellationToken>())
                .Returns(outboundFinSegSequence);

            var finSegIds = _container.Fake<InsertOutboundFinSegFiles.Result>();
            mediator
                .Send(Arg.Any<InsertOutboundFinSegFiles.Command>(),
                    Arg.Any<CancellationToken>())
                .Returns(finSegIds);
            var outboundFinSegFileMessage = new OutboundFinSegFileMessage
            {
                OutboundFinSeg835FileId = finSegIds.OutboundFinSeg835FileId,
                OutboundFinSegTxtFileId = finSegIds.OutboundFinSegTxtFileId
            };

            var publishEndpoint = _container.Get<IPublishEndpoint>();
            await publishEndpoint.Publish(Arg.Is<OutboundFinSegFileMessage>(_ =>
           _.OutboundFinSeg835FileId == outboundFinSegFileMessage.OutboundFinSeg835FileId &&
           _.OutboundFinSegTxtFileId == outboundFinSegFileMessage.OutboundFinSegTxtFileId
            ));


            //Act
            var handler = _container.Get<QueueFinSegFiles.Handler>();

            var result = await handler.Handle(new QueueFinSegFiles.Command(), CancellationToken.None);


            //Assert - Should call the correct things
            result.Should().Be(expectedResult);

            await mediator.Received().Send(
                Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.FinSegBuilt),
                Arg.Any<CancellationToken>());

            result.Should().Be(expectedResult);

            await mediator
                .Received()
                .Send(Arg.Any<GetNextFinSegSequence.Command>(), Arg.Any<CancellationToken>());

            await mediator
                .Received()
                .Send(Arg.Any<InsertOutboundFinSegFiles.Command>(), Arg.Any<CancellationToken>());

            await publishEndpoint
                .Received()
                .Publish(Arg.Any<OutboundFinSegFileMessage>());
        }

        [Fact]
        public async Task ShouldHandlePaymentsDifferentOutboundRefEo()
        {
            //Arrange
            var expectedResult = 2;
            var mediator = _container.Get<IMediator>();
            var payerAlias1 = _container.Fake<string>();
            var outboundRefEo1 = _container.Fake<string>();
            var outboundRefEo2 = _container.Fake<string>();

            var payments1 = _container.FakeMany<GetInboundPaymentByStatus.Result>
            (x => x
            .With(y => y.Status, PaymentStatus.FinSegBuilt)
            .With(y => y.PayerAlias, payerAlias1)
            .With(y => y.OutboundRefEo, outboundRefEo1), 3);
            var payments2 = _container.FakeMany<GetInboundPaymentByStatus.Result>
             (x => x
             .With(y => y.Status, PaymentStatus.FinSegBuilt)
             .With(y => y.PayerAlias, payerAlias1)
             .With(y => y.OutboundRefEo, outboundRefEo2), 3);

            var expectedPayments = new List<GetInboundPaymentByStatus.Result>();
            expectedPayments.AddRange(payments1.ToList());
            expectedPayments.AddRange(payments2.ToList());

            mediator
                .Send(Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.FinSegBuilt),
                    Arg.Any<CancellationToken>())
                .Returns(expectedPayments);

            var outboundFinSegSequence = _container.Fake<OutboundFinSegSequence>();
            mediator
                .Send(Arg.Any<GetNextFinSegSequence.Command>(),
                    Arg.Any<CancellationToken>())
                .Returns(outboundFinSegSequence);

            var finSegIds = _container.Fake<InsertOutboundFinSegFiles.Result>();
            mediator
                .Send(Arg.Any<InsertOutboundFinSegFiles.Command>(),
                    Arg.Any<CancellationToken>())
                .Returns(finSegIds);
            var outboundFinSegFileMessage = new OutboundFinSegFileMessage
            {
                OutboundFinSeg835FileId = finSegIds.OutboundFinSeg835FileId,
                OutboundFinSegTxtFileId = finSegIds.OutboundFinSegTxtFileId
            };

            var publishEndpoint = _container.Get<IPublishEndpoint>();
            await publishEndpoint.Publish(Arg.Is<OutboundFinSegFileMessage>(_ =>
           _.OutboundFinSeg835FileId == outboundFinSegFileMessage.OutboundFinSeg835FileId &&
           _.OutboundFinSegTxtFileId == outboundFinSegFileMessage.OutboundFinSegTxtFileId
            ));


            //Act
            var handler = _container.Get<QueueFinSegFiles.Handler>();

            var result = await handler.Handle(new QueueFinSegFiles.Command(), CancellationToken.None);


            //Assert - Should call the correct things
            result.Should().Be(expectedResult);

            await mediator.Received().Send(
                Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.FinSegBuilt),
                Arg.Any<CancellationToken>());

            result.Should().Be(expectedResult);

            await mediator
                .Received()
                .Send(Arg.Any<GetNextFinSegSequence.Command>(), Arg.Any<CancellationToken>());

            await mediator
                .Received()
                .Send(Arg.Any<InsertOutboundFinSegFiles.Command>(), Arg.Any<CancellationToken>());

            await publishEndpoint
                .Received()
                .Publish(Arg.Any<OutboundFinSegFileMessage>());
        }

        [Fact]
        public async Task ShouldHandlePaymentsDifferentPayerAlias()
        {
            //Arrange
            var expectedResult = 2;
            var mediator = _container.Get<IMediator>();
            var payerAlias1 = _container.Fake<string>();
            var payerAlias2 = _container.Fake<string>();
            var outboundRefEo1 = _container.Fake<string>();
 

            var payments1 = _container.FakeMany<GetInboundPaymentByStatus.Result>
            (x => x
            .With(y => y.Status, PaymentStatus.FinSegBuilt)
            .With(y => y.PayerAlias, payerAlias1)
            .With(y => y.OutboundRefEo, outboundRefEo1), 3);
            var payments2 = _container.FakeMany<GetInboundPaymentByStatus.Result>
             (x => x
             .With(y => y.Status, PaymentStatus.FinSegBuilt)
             .With(y => y.PayerAlias, payerAlias2)
             .With(y => y.OutboundRefEo, outboundRefEo1), 3);

            var expectedPayments = new List<GetInboundPaymentByStatus.Result>();
            expectedPayments.AddRange(payments1.ToList());
            expectedPayments.AddRange(payments2.ToList());

            mediator
                .Send(Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.FinSegBuilt),
                    Arg.Any<CancellationToken>())
                .Returns(expectedPayments);

            var outboundFinSegSequence = _container.Fake<OutboundFinSegSequence>();
            mediator
                .Send(Arg.Any<GetNextFinSegSequence.Command>(),
                    Arg.Any<CancellationToken>())
                .Returns(outboundFinSegSequence);

            var finSegIds = _container.Fake<InsertOutboundFinSegFiles.Result>();
            mediator
                .Send(Arg.Any<InsertOutboundFinSegFiles.Command>(),
                    Arg.Any<CancellationToken>())
                .Returns(finSegIds);
            var outboundFinSegFileMessage = new OutboundFinSegFileMessage
            {
                OutboundFinSeg835FileId = finSegIds.OutboundFinSeg835FileId,
                OutboundFinSegTxtFileId = finSegIds.OutboundFinSegTxtFileId
            };

            var publishEndpoint = _container.Get<IPublishEndpoint>();
            await publishEndpoint.Publish(Arg.Is<OutboundFinSegFileMessage>(_ =>
           _.OutboundFinSeg835FileId == outboundFinSegFileMessage.OutboundFinSeg835FileId &&
           _.OutboundFinSegTxtFileId == outboundFinSegFileMessage.OutboundFinSegTxtFileId
            ));


            //Act
            var handler = _container.Get<QueueFinSegFiles.Handler>();

            var result = await handler.Handle(new QueueFinSegFiles.Command(), CancellationToken.None);


            //Assert - Should call the correct things
            result.Should().Be(expectedResult);

            await mediator.Received().Send(
                Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.FinSegBuilt),
                Arg.Any<CancellationToken>());

            result.Should().Be(expectedResult);

            await mediator
                .Received()
                .Send(Arg.Any<GetNextFinSegSequence.Command>(), Arg.Any<CancellationToken>());

            await mediator
                .Received()
                .Send(Arg.Any<InsertOutboundFinSegFiles.Command>(), Arg.Any<CancellationToken>());

            await publishEndpoint
                .Received()
                .Publish(Arg.Any<OutboundFinSegFileMessage>());
        }

        [Fact]
        public async Task ShouldHandlePaymentsDifferentPayerAliasAndDifferentOutboundRefEo()
        {
            //Arrange
            var expectedResult = 4;
            var mediator = _container.Get<IMediator>();
            var payerAlias1 = _container.Fake<string>();
            var payerAlias2 = _container.Fake<string>();
            var outboundRefEo1 = _container.Fake<string>();
            var outboundRefEo2 = _container.Fake<string>();


            var payments1 = _container.FakeMany<GetInboundPaymentByStatus.Result>
            (x => x
            .With(y => y.Status, PaymentStatus.FinSegBuilt)
            .With(y => y.PayerAlias, payerAlias1)
            .With(y => y.OutboundRefEo, outboundRefEo1), 3);
            var payments2 = _container.FakeMany<GetInboundPaymentByStatus.Result>
             (x => x
             .With(y => y.Status, PaymentStatus.FinSegBuilt)
             .With(y => y.PayerAlias, payerAlias1)
             .With(y => y.OutboundRefEo, outboundRefEo2), 3);
            var payments3 = _container.FakeMany<GetInboundPaymentByStatus.Result>
             (x => x
             .With(y => y.Status, PaymentStatus.FinSegBuilt)
             .With(y => y.PayerAlias, payerAlias2)
             .With(y => y.OutboundRefEo, outboundRefEo1), 3);
            var payments4 = _container.FakeMany<GetInboundPaymentByStatus.Result>
             (x => x
             .With(y => y.Status, PaymentStatus.FinSegBuilt)
             .With(y => y.PayerAlias, payerAlias2)
             .With(y => y.OutboundRefEo, outboundRefEo2), 3);

            var expectedPayments = new List<GetInboundPaymentByStatus.Result>();
            expectedPayments.AddRange(payments1.ToList());
            expectedPayments.AddRange(payments2.ToList());
            expectedPayments.AddRange(payments3.ToList());
            expectedPayments.AddRange(payments4.ToList());

            mediator
                .Send(Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.FinSegBuilt),
                    Arg.Any<CancellationToken>())
                .Returns(expectedPayments);

            var outboundFinSegSequence = _container.Fake<OutboundFinSegSequence>();
            mediator
                .Send(Arg.Any<GetNextFinSegSequence.Command>(),
                    Arg.Any<CancellationToken>())
                .Returns(outboundFinSegSequence);

            var finSegIds = _container.Fake<InsertOutboundFinSegFiles.Result>();
            mediator
                .Send(Arg.Any<InsertOutboundFinSegFiles.Command>(),
                    Arg.Any<CancellationToken>())
                .Returns(finSegIds);
            var outboundFinSegFileMessage = new OutboundFinSegFileMessage
            {
                OutboundFinSeg835FileId = finSegIds.OutboundFinSeg835FileId,
                OutboundFinSegTxtFileId = finSegIds.OutboundFinSegTxtFileId
            };

            var publishEndpoint = _container.Get<IPublishEndpoint>();
            await publishEndpoint.Publish(Arg.Is<OutboundFinSegFileMessage>(_ =>
           _.OutboundFinSeg835FileId == outboundFinSegFileMessage.OutboundFinSeg835FileId &&
           _.OutboundFinSegTxtFileId == outboundFinSegFileMessage.OutboundFinSegTxtFileId
            ));


            //Act
            var handler = _container.Get<QueueFinSegFiles.Handler>();

            var result = await handler.Handle(new QueueFinSegFiles.Command(), CancellationToken.None);


            //Assert - Should call the correct things
            result.Should().Be(expectedResult);

            await mediator.Received().Send(
                Arg.Is<GetInboundPaymentByStatus.Query>(x => x.Status == PaymentStatus.FinSegBuilt),
                Arg.Any<CancellationToken>());

            result.Should().Be(expectedResult);

            await mediator
                .Received()
                .Send(Arg.Any<GetNextFinSegSequence.Command>(), Arg.Any<CancellationToken>());

            await mediator
                .Received()
                .Send(Arg.Any<InsertOutboundFinSegFiles.Command>(), Arg.Any<CancellationToken>());

            await publishEndpoint
                .Received()
                .Publish(Arg.Any<OutboundFinSegFileMessage>());
        }

    }
}
