using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace VPay.Gateway.Common.Guidewire;

public class LoggingEndpointBehaviour : IEndpointBehavior
{
    private readonly IClientMessageInspector _messageInspector;

    public LoggingEndpointBehaviour(IClientMessageInspector messageInspector) =>
        _messageInspector = messageInspector ?? throw new ArgumentNullException(nameof(messageInspector));

    public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
    {
        // NOTE: Intentionally left empty
    }

    public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
    {
        ArgumentNullException.ThrowIfNull(clientRuntime);
        clientRuntime.ClientMessageInspectors.Add(_messageInspector);
    }

    public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
    {
        // NOTE: Intentionally left empty
    }

    public void Validate(ServiceEndpoint endpoint)
    {
        // NOTE: Intentionally left empty
    }
}
