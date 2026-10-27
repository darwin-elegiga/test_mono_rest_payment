using VPay.DocSys.Common.RabbitMQ;
using VPay.Queuing;

namespace VPay.DocSys.Generation.Common.Rabbitmq;

public interface IMessageRepositoryReceiver : IMessageReceiver
{
    void UseMessageRepository<TMessage>(IMessageRepositoryFactory messageRepositoryFactory) where TMessage : class;
}
