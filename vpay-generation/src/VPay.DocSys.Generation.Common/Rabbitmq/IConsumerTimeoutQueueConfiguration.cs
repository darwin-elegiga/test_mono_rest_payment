namespace VPay.DocSys.Generation.Common.Rabbitmq;

public interface IConsumerTimeoutQueueConfiguration
{
    long ConsumerTimeout { get; set; }
}
