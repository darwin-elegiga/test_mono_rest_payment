using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.SmartEnum.AutoFixture;
using AutoFixture;
using FluentAssertions;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Queries;
using VPay.Optum.PaymentRouting.OutboundProcessor.Application.Commands;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.Application.Tests.Commands
{

    /// <summary>
    /// Tests/Facts for/about the CreateCtrStatusPublishCommand
    /// </summary>
    /// <remarks>
    ///  Flow:
    ///     Get # transactions from GetOutbound835FileTransactionIdsByOutbound835FileId.Query (db call)
    ///     if transactions == 0
    ///         loginfo "CreateCtrStatusPublish: Unable to find Outbound835FileId to publish to CreateCtrStatus"
    ///
    ///     foreach
    ///         setup log scope
    ///          if (txn.Outbound835FileStatus != Outbound835FileStatus.Completed)
    ///             loginfo "CreateCtrStatusPublish: Unable to publish transaction, not in Completed status"
    ///             continue
    ///         sentDateTime = txn.Outbound835FileSentDateTime or txn.Outbound835FileCreatedDateTime
    ///         count++
    ///         Publish CreateCtrStatus(txn.TransactionId, CtrStatus.SentOptum, sentDateTime)
    ///         loginfo "CreateCtrStatusPublish: Published to queue"
    ///
    ///     return count(number of successfully published transactions)
    /// </remarks>
    public class CreateCtrStatusPublishCommandFacts
    {
        private readonly PaymentRoutingTestingContainer _container;
        public CreateCtrStatusPublishCommandFacts()
        {
            _container = new PaymentRoutingTestingContainer(services =>
                {
                    services.AddSingleton<CreateCtrStatusPublishCommand.Handler>()
                    .AddMock<ILogger<CreateCtrStatusPublishCommand.Handler>>()
                    .AddMock<IOptumWrapBus>()
                    .AddMock<IMediator>()
                    .AddMock<IPublishEndpoint>();
                });
            var fixture = _container.Get<Fixture>();
            fixture.Customize(new SmartEnumCustomization());
            fixture.Create<Outbound835FileStatus>();
        }
 
        [Fact]
        public async Task ShouldHandleNoOutboundFileFound()
        {
            // Arrange
            var expectedCount = 0;
            var mediator = _container.Get<IMediator>();
            var logger = _container.Get<ILogger<CreateCtrStatusPublishCommand.Handler>>();
            // should return empty resultset
            var expectedTransactions = Array.Empty<GetOutbound835FileTransactionIdsByOutbound835FileId.Result>();
            _ = mediator
                .Send(Arg.Any<GetOutbound835FileTransactionIdsByOutbound835FileId.Query>(), Arg.Any<CancellationToken>())
                .Returns(expectedTransactions);

            var publisher = _container.Get<IPublishEndpoint>();

            // Act
            var handler = _container.Get<CreateCtrStatusPublishCommand.Handler>();
            var result = await handler.Handle(new CreateCtrStatusPublishCommand.Command(1), CancellationToken.None);
            // Assert
            result.Should().Be(expectedCount);
            publisher.DidNotReceive();



        }

        [Fact]
        public async Task ShouldHandleOutboundFileInNonCompleteStatus()
        {
            // Arrange
            var expectedCount = 0;
            var mediator = _container.Get<IMediator>();
            var logger = _container.Get<ILogger<CreateCtrStatusPublishCommand.Handler>>();
            // should return empty resultset
            var txn1 = _container.Fake<GetOutbound835FileTransactionIdsByOutbound835FileId.Result>(x => x
                .With(y => y.Outbound835FileStatus, Outbound835FileStatus.Transferring));

            var expectedTransactions = new List<GetOutbound835FileTransactionIdsByOutbound835FileId.Result>();
            expectedTransactions.Add(txn1);
            _ = mediator
                .Send(Arg.Any<GetOutbound835FileTransactionIdsByOutbound835FileId.Query>(), Arg.Any<CancellationToken>())
                .Returns(expectedTransactions);

            var publisher = _container.Get<IPublishEndpoint>();

            // Act
            var handler = _container.Get<CreateCtrStatusPublishCommand.Handler>();
            var result = await handler.Handle(new CreateCtrStatusPublishCommand.Command(1), CancellationToken.None);
            // Assert
            result.Should().Be(expectedCount);
            publisher.DidNotReceive();
        }

        [Fact]
        public async Task ShouldHandleOutboundFileInCompleteStatus()
        {

            // Arrange
            var expectedCount = 1;
            var mediator = _container.Get<IMediator>();
            var logger = _container.Get<ILogger<CreateCtrStatusPublishCommand.Handler>>();
            // should return empty resultset


            // So... I couldn't get the SmartEnum (that works PERFECTLY) with PaymentStatus to work here without making the constructor public...
            var txn1 = _container.Fake<GetOutbound835FileTransactionIdsByOutbound835FileId.Result>(x => x
                .With(y => y.Outbound835FileStatus, Outbound835FileStatus.Completed));

            var expectedTransactions = new List<GetOutbound835FileTransactionIdsByOutbound835FileId.Result>();
            expectedTransactions.Add(txn1);
            _ = mediator
                .Send(Arg.Any<GetOutbound835FileTransactionIdsByOutbound835FileId.Query>(), Arg.Any<CancellationToken>())
                .Returns(expectedTransactions);

            var publisher = _container.Get<IPublishEndpoint>();

            // Act
            var handler = _container.Get<CreateCtrStatusPublishCommand.Handler>();
            var result = await handler.Handle(new CreateCtrStatusPublishCommand.Command(1), CancellationToken.None);
            // Assert
            result.Should().Be(expectedCount);
            publisher.DidNotReceive();
        }


    }
}
