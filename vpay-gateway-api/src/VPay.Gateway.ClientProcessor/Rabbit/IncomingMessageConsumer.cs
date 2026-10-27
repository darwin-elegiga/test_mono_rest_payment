using MassTransit;
using Vpay.Pep.Kubera.Core.Messages;
using VPay.Gateway.Common.Commands;

namespace VPay.Gateway.ClientProcessor.Rabbit;

/// <summary>
/// Consumer for messages from PepKubera.
/// </summary>
public class IncomingMessageConsumer : IConsumer<IIncomingMessage>
{
    // NOTE: We need to use the IBus rather than the IPublishEndpoint to publish to the correct exchange
    private readonly IBus _bus;

    public IncomingMessageConsumer(IBus bus)
    {
        _bus = bus;
    }

    public async Task Consume(ConsumeContext<IIncomingMessage> context)
    {
        var claimIds = context.Message.PepClaimIds?.ToList();
        if (claimIds?.Count > 0)
        {
            foreach (var id in claimIds.Distinct())
            {
                await _bus.Publish(new ProcessPepClaimRecord { ClaimId = Convert.ToInt64(id) }, x => x.Durable = true);
            }
        }
    }
}
