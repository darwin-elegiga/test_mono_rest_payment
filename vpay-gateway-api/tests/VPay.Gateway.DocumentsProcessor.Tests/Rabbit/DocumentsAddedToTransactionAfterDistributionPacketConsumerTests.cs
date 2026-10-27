using GreenPipes;

using MassTransit;
using MediatR;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;
using VPay.EventBus.Messages.DocSys;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.DocumentsProcessor.Abstractions;
using VPay.Gateway.DocumentsProcessor.Consumer.Rabbit;

using Xunit;

namespace VPay.Gateway.DocumentsProcessor.Tests.Rabbit;

public class DocumentsAddedToTransactionAfterDistributionPacketConsumerTests : DocumentMessageNotificationConsumerBaseTests<DocumentsAddedToTransactionAfterDistributionPacketConsumer, DocumentsAddedToTransactionAfterDistributionPacket>
{
    private readonly Mock<IBus> mockIBus = new Mock<IBus>();
    private readonly Mock<IMediator> _mockMediator = new Mock<IMediator>();

    public DocumentsAddedToTransactionAfterDistributionPacketConsumerTests()
    {
        mockIBus
            .Setup(c => c.Publish(
                It.IsAny<GuidewireDocumentNotification>(),
                It.IsAny<IPipe<PublishContext<GuidewireDocumentNotification>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();
    }

    protected override DocumentsAddedToTransactionAfterDistributionPacketConsumer CreateConsumer() =>
        new DocumentsAddedToTransactionAfterDistributionPacketConsumer(
            new NullLogger<DocumentsAddedToTransactionAfterDistributionPacketConsumer>(),
            mockIBus.Object,
            _mockMediator.Object);

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(1)]
    public async Task ShouldPublishToBusSameNumberOfFilesInMessage(int? count)
    {
        List<FileInformation> files = count == null ? null : new List<FileInformation>(count.Value);

        for (int i = 0; i < (count ?? 0); i++)
        {
            files.Add(new FileInformation { FileDetailType = FileDetailType.Generic });
        }

        _mockMediator
            .Setup(x => x.Send(It.IsAny<ExternalClientConfigurationQuery>(), CancellationToken.None))
            .ReturnsAsync(new ExternalClientConfigurationDto())
            .Verifiable();

        var message = new Mock<ConsumeContext<DocumentsAddedToTransactionAfterDistributionPacket>>();
        message
            .Setup(c => c.Message)
            .Returns(new DocumentsAddedToTransactionAfterDistributionPacket { Files = files });

        await SUT.Consume(message.Object);

        mockIBus.Verify(c => c.Publish(
            It.IsAny<GuidewireDocumentNotification>(),
            It.IsAny<IPipe<PublishContext<GuidewireDocumentNotification>>>(),
            It.IsAny<CancellationToken>()),
            count == null ? Times.Never() : Times.Exactly(count.Value));
    }

    [Fact]
    public async Task WhenClientNotConfiguredToSendToGuidewire_ShouldNotSend()
    {
        var message = new Mock<ConsumeContext<DocumentsAddedToTransactionAfterDistributionPacket>>();
        message
            .Setup(c => c.Message)
            .Returns(new DocumentsAddedToTransactionAfterDistributionPacket { Files = new List<FileInformation>(1) { new FileInformation() } });

        _mockMediator
            .Setup(x => x.Send(It.IsAny<ExternalClientConfigurationQuery>(), CancellationToken.None))
            .ReturnsAsync((ExternalClientConfigurationDto)null)
            .Verifiable();

        await SUT.Consume(message.Object).ConfigureAwait(false);

        _mockMediator.VerifyAll();
        mockIBus.Verify(c => c.Publish(
            It.IsAny<GuidewireDocumentNotification>(),
            It.IsAny<IPipe<PublishContext<GuidewireDocumentNotification>>>(),
            It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task WhenDocumentsAreExcludedFromSendToGuidewire_ShouldNotSend()
    {
        const string ClientCode = "Client";
        const string BillingEntityCode = "BillingEntity";
        var billingEntity = new BillingEntityDto { Type = new BillingEntityTypeDto { Name = "MEDICAL" } };
        var message = new Mock<ConsumeContext<DocumentsAddedToTransactionAfterDistributionPacket>>();
        message
            .SetupGet(c => c.Message)
            .Returns(new DocumentsAddedToTransactionAfterDistributionPacket
            {
                ClientCode = ClientCode,
                BillingEntityCode = BillingEntityCode,
                Files = new List<FileInformation>(5)
                {
                        new FileInformation { FileDetailType = FileDetailType.Hidden },
                        new FileInformation { FileDetailType = FileDetailType.Limited },
                        new FileInformation { FileDetailType = FileDetailType.Text },
                        new FileInformation { FileDetailType = FileDetailType.BundledFax },
                        new FileInformation { FileDetailType = FileDetailType.FaxDocument }
                }
            });

        _mockMediator
            .Setup(x => x.Send(It.IsAny<ExternalClientConfigurationQuery>(), CancellationToken.None))
            .ReturnsAsync(new ExternalClientConfigurationDto())
            .Verifiable();

        _mockMediator
            .Setup(x => x.Send(It.Is<BillingEntityQuery>(query => query.ClientCode == ClientCode && query.BillingEntityCode == BillingEntityCode), CancellationToken.None))
            .ReturnsAsync(billingEntity)
            .Verifiable();

        await SUT.Consume(message.Object).ConfigureAwait(false);

        _mockMediator.VerifyAll();
        mockIBus.Verify(c => c.Publish(
                It.IsAny<GuidewireDocumentNotification>(),
                It.IsAny<IPipe<PublishContext<GuidewireDocumentNotification>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never());
    }
}
