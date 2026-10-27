using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using MassTransit.EndpointConfigurators;
using MassTransit.Pipeline;
using MassTransit.Topology;
using VPay.FileTransfer.Messaging;

namespace VPay.DocSys.Parsing.Tests.Helpers;

[ExcludeFromCodeCoverage]
public class FileTransferServiceBus : IFileTransferServiceBus
{
    private readonly IBus _bus;

    public FileTransferServiceBus(IBus bus) => _bus = bus;

    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _bus.ConnectPublishObserver(observer);
    }

    public Task<ISendEndpoint> GetPublishSendEndpoint<T>() where T : class
    {
        return _bus.GetPublishSendEndpoint<T>();
    }

    public Task Publish<T>(T message, CancellationToken cancellationToken = new()) where T : class
    {
        return _bus.Publish(message, cancellationToken);
    }

    public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = new()) where T : class
    {
        return _bus.Publish(message, publishPipe, cancellationToken);
    }

    public Task Publish<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = new()) where T : class
    {
        return _bus.Publish(message, publishPipe, cancellationToken);
    }

    public Task Publish(object message, CancellationToken cancellationToken = new())
    {
        return _bus.Publish(message, cancellationToken);
    }

    public Task Publish(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = new())
    {
        return _bus.Publish(message, publishPipe, cancellationToken);
    }

    public Task Publish(object message, Type messageType, CancellationToken cancellationToken = new())
    {
        return _bus.Publish(message, messageType, cancellationToken);
    }

    public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = new())
    {
        return _bus.Publish(message, messageType, publishPipe, cancellationToken);
    }

    public Task Publish<T>(object values, CancellationToken cancellationToken = new()) where T : class
    {
        return _bus.Publish<T>(values, cancellationToken);
    }

    public Task Publish<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = new()) where T : class
    {
        return _bus.Publish(values, publishPipe, cancellationToken);
    }

    public Task Publish<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = new()) where T : class
    {
        return _bus.Publish<T>(values, publishPipe, cancellationToken);
    }

    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _bus.ConnectSendObserver(observer);
    }

    public Task<ISendEndpoint> GetSendEndpoint(Uri address)
    {
        return _bus.GetSendEndpoint(address);
    }

    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe) where T : class
    {
        return _bus.ConnectConsumePipe(pipe);
    }

    public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe) where T : class
    {
        return _bus.ConnectRequestPipe(requestId, pipe);
    }

    public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer) where T : class
    {
        return _bus.ConnectConsumeMessageObserver(observer);
    }

    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        return _bus.ConnectConsumeObserver(observer);
    }

    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        return _bus.ConnectReceiveObserver(observer);
    }

    public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        return _bus.ConnectReceiveEndpointObserver(observer);
    }

    public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
    {
        return _bus.ConnectEndpointConfigurationObserver(observer);
    }

    public HostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter endpointNameFormatter, Action<IReceiveEndpointConfigurator> configureEndpoint = null)
    {
        return _bus.ConnectReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    public HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configureEndpoint)
    {
        return _bus.ConnectReceiveEndpoint(queueName, configureEndpoint);
    }

    public void Probe(ProbeContext context)
    {
        _bus.Probe(context);
    }

    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options) where T : class
    {
        return _bus.ConnectConsumePipe(pipe, options);
    }

    public Uri Address => _bus.Address;

    public IBusTopology Topology => _bus.Topology;
}
