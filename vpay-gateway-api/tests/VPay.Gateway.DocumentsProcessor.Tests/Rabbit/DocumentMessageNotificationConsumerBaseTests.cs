using MassTransit;
using Moq;
using VPay.EventBus.Abstractions;
using VPay.EventBus.Messages.DocSys;
using VPay.Gateway.DocumentsProcessor.Consumer.Rabbit;
using Xunit;

namespace VPay.Gateway.DocumentsProcessor.Tests.Rabbit;

public abstract class DocumentMessageNotificationConsumerBaseTests<TConsumer, TDocumentMessageType>
    where TDocumentMessageType : DocumentMessageBase, IEventBusMessage
    where TConsumer : DocumentMessageNotificationConsumerBase<TDocumentMessageType>
{
    private TConsumer _sut;

    protected TConsumer SUT => _sut ?? (_sut = CreateConsumer());

    public static IEnumerable<object[]> NullTests
    {
        get
        {
            yield return new object[] { typeof(NullReferenceException), null };
            yield return new object[] { typeof(NullReferenceException), Mock.Of<ConsumeContext<TDocumentMessageType>>() };
        }
    }

    protected abstract TConsumer CreateConsumer();

    [Theory]
    [MemberData(nameof(NullTests))]
    public async Task ShouldThrowExceptionOnNull(Type exception, ConsumeContext<TDocumentMessageType> context)
    {
        await Assert.ThrowsAsync(exception, () => SUT.Consume(context));
    }
}
