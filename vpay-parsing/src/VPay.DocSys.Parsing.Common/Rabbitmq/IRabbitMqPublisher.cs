using System;

namespace VPay.DocSys.Parsing.Common.Rabbitmq;

public interface IRabbitMqPublisher : IDisposable
{
    void Publish(string message);

    void PublishUsingMessageRepository<T>(T message) where T : class;

    bool IsConnected();

    uint DeleteQueue();
}
