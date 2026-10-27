using MassTransit;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using VPay.EventBus.Abstractions;
using VPay.EventBus.Messages.DocSys;
using VPay.Gateway.DocumentsProcessor.Consumer.Rabbit;

using Xunit;

namespace VPay.Gateway.DocumentsProcessor.Tests.Rabbit;

public class DocumentsDeletedConsumerTests
{
    private readonly IEventBusConsumer<DocumentsDeleted> _sut;

    public DocumentsDeletedConsumerTests()
    {
        _sut = new DocumentsDeletedConsumer(new NullLogger<DocumentsDeletedConsumer>());
    }

    [Theory]
    [MemberData(nameof(NullTests))]
    public async Task ShouldThrowExceptionOnNull(Type exception, ConsumeContext<DocumentsDeleted> context)
    {
        await Assert.ThrowsAsync(exception, () => _sut.Consume(context));
    }

    public static IEnumerable<object[]> NullTests
    {
        get
        {
            yield return new object[] { typeof(NullReferenceException), null };
            yield return new object[] { typeof(NullReferenceException), Mock.Of<ConsumeContext<DocumentsDeleted>>() };
        }
    }
}
