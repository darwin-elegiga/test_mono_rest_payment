using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.Testing;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json.Linq;
using VPay.FileTransfer.Messaging;
using VPay.FileTransfer.Messaging.Abstractions;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.Models;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Queries;
using VPay.Optum.PaymentRouting.OutboundProcessor.Application.Commands;
using VPay.Optum.PaymentRouting.OutboundProcessor.Consumers.Optum835PostTransferEvent;
using Xunit;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.Tests.Consumers.Outbound835
{
    public static class Constants
    {
        public const string FileSource = "TEST_TRANSFER_SOURCE";
        public const string FileDestination = "TEST_TRANSFER_DESTINATION";
    }

    public class Optum835PostTransferEventMessageConsumerTests
    {
        /// <summary>
        /// Tests for the "Constructor" of the <see cref="PostTransferEventMessageConsumer"/>
        /// </summary>
        public class Constructor
        {
#pragma warning disable CA1806

            [Fact]
            public void WhenMediatorIsNull_ThrowsArgumentNullException() =>
                new Action(() => new Optum835PostTransferEventMessageConsumer(null!, Mock.Of<IFileReadyForTransferNotificationPublisher>(), new NullLogger<Optum835PostTransferEventMessageConsumer>()))
                    .Should().ThrowExactly<ArgumentNullException>()
                    .And.ParamName.Should().Be("mediator");

            [Fact]
            public void WhenFileReadyForTransferNotificationPublisherIsNull_ThrowsArgumentNullException() =>
                new Action(() => new Optum835PostTransferEventMessageConsumer(Mock.Of<IMediator>(), null!, new NullLogger<Optum835PostTransferEventMessageConsumer>()))
                    .Should().ThrowExactly<ArgumentNullException>()
                    .And.ParamName.Should().Be("fileReadyForTransferNotificationPublisher");

            [Fact]
            public void WhenLoggerIsNull_ThrowsArgumentNullException() =>
                new Action(() => new Optum835PostTransferEventMessageConsumer(Mock.Of<IMediator>(), Mock.Of<IFileReadyForTransferNotificationPublisher>(), null!))
                    .Should().ThrowExactly<ArgumentNullException>()
                    .And.ParamName.Should().Be("logger");

#pragma warning restore CA1806
        }

        public class Consume : MassTransitTestHarnessBase
        {
            private const string FileName = "test.txt";
            private const string FileHash = "ThisIsATestFileHash";
            private const string PrivateKeyPath = "/some/test/path/key.ext";

            private readonly Mock<ILogger<Optum835PostTransferEventMessageConsumer>> _loggerMock;
            private readonly Mock<IMediator> _mockMediator = new(MockBehavior.Strict);
            private readonly Mock<IFileReadyForTransferNotificationPublisher> _mockFileReadyForTransferNotificationPublisher = new(MockBehavior.Strict);

            public Consume(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
            {
                _loggerMock = CreateMockLogger<Optum835PostTransferEventMessageConsumer>();

                TransferredDateTime = DateTime.Now;
            }

            private readonly Mock<Func<PostTransferEventMessage, object>> _loadTrackingRecordMock = new(MockBehavior.Strict);
            private readonly Mock<Func<PostTransferEventMessage, object, bool>> _shouldMessageBePublishedMock = new(MockBehavior.Strict);
            private readonly Mock<Action<PostTransferEventMessage, object, TransferStatus>> _updateFileTrackingStatusMock = new(MockBehavior.Strict);

            private DateTime TransferredDateTime { get; }

            protected override async ValueTask DisposeAsyncCore()
            {
                if (IsDisposed || IsDisposedAsync)
                {
                    return;
                }

                await base.DisposeAsyncCore().ConfigureAwait(false);

                _mockMediator.VerifyAll();
                _mockFileReadyForTransferNotificationPublisher.VerifyAll();

                _loadTrackingRecordMock.VerifyAll();
                _shouldMessageBePublishedMock.VerifyAll();
                _updateFileTrackingStatusMock.VerifyAll();
            }

            protected override void ConfigureServices(IServiceCollection services)
            {
                services
                    .Configure<SigningServiceOptions>(options => options.PrivateKeyPath = PrivateKeyPath)
                    .AddTransient(cfg => cfg.GetService<IOptions<SigningServiceOptions>>()!.Value)
                    .AddSingleton(_ => _loggerMock.Object)
                    .AddSingleton(_ => _mockMediator.Object)
                    .AddSingleton(_ => _mockFileReadyForTransferNotificationPublisher.Object)
                    .AddSingleton(sp =>
                    {
                        var consumer = ActivatorUtilities.CreateInstance<Optum835PostTransferEventMessageConsumer>(sp);
                        return consumer;
                    });
            }

            private TestHarness ConfigureTestHarness()
            {
                var harness = CreateTestHarness();
                harness.Harness.OnConfigureInMemoryBus += configurator => configurator.UseRawJsonSerializer();
                return harness;
            }

            protected override void ConfigureBus(IServiceCollectionBusConfigurator cfg)
            {
                base.ConfigureBus(cfg);
                cfg.AddConsumer<Optum835PostTransferEventMessageConsumer, Optum835PostTransferEventMessageConsumerDefinition>();
                cfg.AddConsumerTestHarness<Optum835PostTransferEventMessageConsumer>();
            }

            private IConsumerTestHarness<Optum835PostTransferEventMessageConsumer> ConfigureConsumerTestHarness(TestHarness harness)
            {
                return harness.Harness.Consumer(() => Provider.GetRequiredService<Optum835PostTransferEventMessageConsumer>());
            }

            [Fact]
            public async Task WorksAsExpected()
            {
                _mockMediator.Setup(_ => _.Send(It.IsAny<GetOutbound835FileById.Query>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Outbound835File
                    {
                        Status = Outbound835FileStatus.Queued
                    });

                _mockMediator.Setup(_ => _.Send(It.IsAny<UpdateOutbound835FileStatus.Command>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Outbound835File());


                _mockMediator.Setup(_ => _.Send(It.IsAny<CreateCtrStatusPublishCommand.Command>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(2);

                await using var harness = ConfigureTestHarness();
                var consumerTestHarness = ConfigureConsumerTestHarness(harness);
                await harness.Start().ConfigureAwait(false);

                await PublishMessage(harness, "SUCCESS").ConfigureAwait(false);

                // verify the message went through the bus and consumer correctly
                await harness.VerifyMessageWasPublishedAndConsumed<PostTransferEventMessage>().ConfigureAwait(false);
                await harness.VerifyConsumerConsumedMessage<Optum835PostTransferEventMessageConsumer, PostTransferEventMessage>(consumerTestHarness).ConfigureAwait(false);
                await harness.VerifyMessageWasNotFaulted<PostTransferEventMessage>().ConfigureAwait(false);
                _mockMediator.Verify(_ => _.Send(It.IsAny<UpdateOutbound835FileStatus.Command>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            }

            [Fact]
            public async Task WhenStatusShouldntChange_DontUpdate()
            {
                _mockMediator.Setup(_ => _.Send(It.IsAny<GetOutbound835FileById.Query>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Outbound835File
                    {
                        Status = Outbound835FileStatus.Completed
                    });

                await using var harness = ConfigureTestHarness();
                var consumerTestHarness = ConfigureConsumerTestHarness(harness);
                await harness.Start().ConfigureAwait(false);

                await PublishMessage(harness, "SUCCESS").ConfigureAwait(false);

                // verify the message went through the bus and consumer correctly
                await harness.VerifyMessageWasPublishedAndConsumed<PostTransferEventMessage>().ConfigureAwait(false);
                await harness.VerifyConsumerConsumedMessage<Optum835PostTransferEventMessageConsumer, PostTransferEventMessage>(consumerTestHarness).ConfigureAwait(false);
                await harness.VerifyMessageWasNotFaulted<PostTransferEventMessage>().ConfigureAwait(false);
            }

            #region Setup Helpers

            private async Task PublishMessage(
                TestHarness harness,
                string transferStatus,
                int retries = 0)
            {
                var parameters = new JObject
                {
                    ["fileId"] = 100
                };

                if (retries > 0)
                {
                    parameters["retries"] = retries;
                }

                await harness.Harness.Bus.Publish(
                    new PostTransferEventMessage(
                        TransferStatus: transferStatus,
                        TransferredDateTime: TransferredDateTime,
                        OriginalMessage: new FileTransferMessage()
                        {
                            Source = Constants.FileSource,
                            Destination = Constants.FileDestination,
                            FileName = FileName,
                            FileHash = FileHash,
                            Parameters = parameters
                        })
                ).ConfigureAwait(false);
            }

            private void SetupFileReadyForTransferNotification(int expectedRetries)
            {
                _mockFileReadyForTransferNotificationPublisher
                    .Setup(x => x.Publish(It.Is<FileReadyForTransferNotification>(q =>
                            q.Source == Constants.FileSource
                            && q.Destination == Constants.FileDestination
                            && q.FileName == FileName
                            && q.SignedFileHash == FileHash
                            && q.AdditionalProperties!.ContainsKey("retries")
                            && q.AdditionalProperties.Value<int>("retries") == expectedRetries),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask)
                    .Verifiable();
            }

            #endregion Setup Helpers
        }
    }
}
