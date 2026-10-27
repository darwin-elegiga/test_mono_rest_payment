using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json.Linq;
using VPay.DocSys.Parsing.Tests.Helpers;
using VPay.FileTransfer.Messaging;
using VPay.FileTransfer.Messaging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace VPay.DocSys.Parsing.Tests;

public class PaymentEngineConfirmationFileReadyForTransferNotificationHandlerTests
{
    public class Constructor
    {
#pragma warning disable CA1806
        [Fact]
        public void WhenBusIsNull_ThrowsArgumentNullException() =>
            new Action(() => new PaymentEngineConfirmationFileReadyForTransferNotificationHandler(null!,
                    new NullLogger<PaymentEngineConfirmationFileReadyForTransferNotificationHandler>()))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("fileTransferServiceBus");
        [Fact]
        public void WhenLoggerIsNull_ThrowsArgumentNullException() =>
            new Action(() => new PaymentEngineConfirmationFileReadyForTransferNotificationHandler(Mock.Of<IFileTransferServiceBus>(),
                    null!))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("logger");

#pragma warning restore CA1806
    }

    public class Consume : MassTransitTestHarnessBase
    {
        private const string FileName = "test.txt";
        private const string SignedFileHash = "ThisIsATestFileHash_Signed";

        private readonly Mock<ILogger<PaymentEngineConfirmationFileReadyForTransferNotificationHandler>> _loggerMock;

        public Consume(ITestOutputHelper testOutputHelper) : base(testOutputHelper) =>
            _loggerMock = CreateMockLogger<PaymentEngineConfirmationFileReadyForTransferNotificationHandler>();

        protected override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSingleton(_ => _loggerMock.Object)
                .ConfigureFileReadyForTransferNotificationPublisher(cfg =>
                {
                    cfg.AddConsumer<PaymentEngineConfirmationFileReadyForTransferNotificationHandler>();
                })
                .AddTransient<IFileTransferServiceBus, FileTransferServiceBus>();
        }

        private TestHarness ConfigureTestHarness()
        {
            var harness = CreateTestHarness();
            harness.Harness.OnConfigureInMemoryBus += configurator => configurator.UseRawJsonSerializer();
            return harness;
        }

        [Fact]
        public async Task WorksAsExpected()
        {
            await using var harness = ConfigureTestHarness();
            await harness.Start().ConfigureAwait(false);

            await PublishFileReadyForTransferNotification().ConfigureAwait(false);

            // verify the message went through the bus
            await harness.VerifyMessageWasPublished<FileTransferMessage>();

            // there should only be one message published, so use Single() here so we fail if there are 0 or more
            var publishedMessage = harness.Harness.Published.Select<FileTransferMessage>().Single().Context.Message;
            VerifyFileTransferMessage(publishedMessage);
        }

        private async Task PublishFileReadyForTransferNotification()
        {
            var publisher = Provider.GetRequiredService<IFileReadyForTransferNotificationPublisher>();
            await publisher.Publish(new FileReadyForTransferNotification(
                    Constants.FileSource,
                    Constants.FileDestination,
                    FileName,
                    SignedFileHash,
                    new JObject(new JProperty("trackingId", FileName))
                ),
                CancellationToken.None
            ).ConfigureAwait(false);
        }

        private static void VerifyFileTransferMessage(FileTransferMessage notification)
        {
            using (new AssertionScope())
            {
                notification.FileName.Should().Be(FileName);
                notification.FileHash.Should().Be(SignedFileHash);
                notification.Source.Should().Be(Constants.FileSource);
                notification.Destination.Should().Be(Constants.FileDestination);
                notification.Parameters.Should().BeEquivalentTo(new JObject(new JProperty("trackingId", FileName)));
            }
        }

    }
}
