using System;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json.Linq;
using VPay.DocSys.Parsing.Tests.Helpers;
using VPay.FileTransfer.Messaging;
using VPay.FileTransfer.Messaging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace VPay.DocSys.Parsing.Tests;

public static class Constants
{
    public const string FileSource = "OPTUM_PE";
    public const string FileDestination = "PE";
}

public class PostPaymentEngineConfirmationEventMessageConsumerTests
{
    public class Constructor
    {
#pragma warning disable CA1806
        [Fact]
        public void WhenPublisherIsNull_ThrowsArgumentNullException() =>
            new Action(() => new PostPaymentEngineConfirmationEventMessageConsumer(null!,
                    new NullLogger<PostPaymentEngineConfirmationEventMessageConsumer>()))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("fileReadyForTransferNotificationPublisher");
        [Fact]
        public void WhenLoggerIsNull_ThrowsArgumentNullException() =>
            new Action(() => new PostPaymentEngineConfirmationEventMessageConsumer(Mock.Of<IFileReadyForTransferNotificationPublisher>(),
                    null!))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("logger");

#pragma warning restore CA1806
    }

    public class Consume : MassTransitTestHarnessBase
    {
        private const string FileName = "test.txt";
        private const string FileHash = "ThisIsATestFileHash";
        private const string PrivateKeyPath = "/some/test/path/key.ext";

        private DateTime TransferredDateTime { get; }
        private readonly Mock<ILogger<PostPaymentEngineConfirmationEventMessageConsumer>> _loggerMock;
        private readonly Mock<IFileReadyForTransferNotificationPublisher> _mockFileReadyForTransferNotificationPublisher = new(MockBehavior.Strict);
        private readonly Mock<Func<PostTransferEventMessage, object>> _loadTrackingRecordMock = new(MockBehavior.Strict);
        private readonly Mock<Func<PostTransferEventMessage, object, bool>> _shouldMessageBePublishedMock = new(MockBehavior.Strict);
        private readonly Mock<Action<PostTransferEventMessage, object, TransferStatus>> _updateFileTrackingStatusMock = new(MockBehavior.Strict);

        public Consume(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            _loggerMock = CreateMockLogger<PostPaymentEngineConfirmationEventMessageConsumer>();

            TransferredDateTime = DateTime.Now;
        }

        [Fact]
        public async Task WorksAsExpected()
        {
            await using var harness = ConfigureTestHarness();
            var consumerTestHarness = ConfigureConsumerTestHarness(harness);
            await harness.Start().ConfigureAwait(false);

            await PublishMessage(harness, "SUCCESS").ConfigureAwait(false);

            // verify the message went through the bus and consumer correctly
            await harness.VerifyMessageWasPublishedAndConsumed<PostTransferEventMessage>().ConfigureAwait(false);
            await harness.VerifyConsumerConsumedMessage<PostPaymentEngineConfirmationEventMessageConsumer, PostTransferEventMessage>(consumerTestHarness).ConfigureAwait(false);
            await harness.VerifyMessageWasNotFaulted<PostTransferEventMessage>().ConfigureAwait(false);
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            if (IsDisposed || IsDisposedAsync)
            {
                return;
            }

            await base.DisposeAsyncCore().ConfigureAwait(false);

            _mockFileReadyForTransferNotificationPublisher.VerifyAll();

            _loadTrackingRecordMock.VerifyAll();
            _shouldMessageBePublishedMock.VerifyAll();
            _updateFileTrackingStatusMock.VerifyAll();
        }

        protected override void ConfigureBus(IServiceCollectionBusConfigurator cfg)
        {
            base.ConfigureBus(cfg);
            cfg.AddConsumer<PostPaymentEngineConfirmationEventMessageConsumer, PostPaymentEngineConfirmationEventMessageConsumerDefinition>();
            cfg.AddConsumerTestHarness<PostPaymentEngineConfirmationEventMessageConsumer>();
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            services
                .Configure<SigningServiceOptions>(options => options.PrivateKeyPath = PrivateKeyPath)
                .AddTransient(cfg => cfg.GetService<IOptions<SigningServiceOptions>>()!.Value)
                .AddSingleton(_ => _loggerMock.Object)
                .AddSingleton(_ => _mockFileReadyForTransferNotificationPublisher.Object)
                .AddSingleton(sp =>
                {
                    var consumer = ActivatorUtilities.CreateInstance<PostPaymentEngineConfirmationEventMessageConsumer>(sp);
                    return consumer;
                });
        }

        private TestHarness ConfigureTestHarness()
        {
            var harness = CreateTestHarness();
            harness.Harness.OnConfigureInMemoryBus += configurator => configurator.UseRawJsonSerializer();
            return harness;
        }

        private IConsumerTestHarness<PostPaymentEngineConfirmationEventMessageConsumer> ConfigureConsumerTestHarness(TestHarness harness)
        {
            return harness.Harness.Consumer(() => Provider.GetRequiredService<PostPaymentEngineConfirmationEventMessageConsumer>());
        }

        private async Task PublishMessage(
            TestHarness harness,
            string transferStatus,
            int retries = 0)
        {
            var parameters = new JObject
            {
                ["trackingId"] = FileName
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
    }
}
