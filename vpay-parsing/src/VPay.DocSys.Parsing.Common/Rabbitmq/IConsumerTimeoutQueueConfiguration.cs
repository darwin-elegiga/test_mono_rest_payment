namespace VPay.DocSys.Parsing.Common.Rabbitmq
{
    public interface IConsumerTimeoutQueueConfiguration
    {
        long ConsumerTimeout { get; set; }
    }
}
