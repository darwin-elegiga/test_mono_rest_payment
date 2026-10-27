using VPay.DocSys.Generation.Common.Rabbitmq;
using VPay.Queuing;

namespace VPay.DocSys.Generation.Models
{
    public class DocumentGenerationQueueConfigurationSettings : QueueConfigurationSettings, IConsumerTimeoutQueueConfiguration
    {
        public long ConsumerTimeout { get; set; }
    }
}
