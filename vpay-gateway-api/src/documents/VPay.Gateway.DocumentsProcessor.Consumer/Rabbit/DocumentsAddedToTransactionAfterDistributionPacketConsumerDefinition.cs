using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPay.MassTransit.RabbitMqTransport;

namespace VPay.Gateway.DocumentsProcessor.Consumer.Rabbit;

public class DocumentsAddedToTransactionAfterDistributionPacketConsumerDefinition : QuorumConsumerDefinition<DocumentsAddedToTransactionAfterDistributionPacketConsumer>
{
}
