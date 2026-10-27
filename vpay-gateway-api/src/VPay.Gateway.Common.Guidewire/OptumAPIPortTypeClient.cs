using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;

namespace VPay.Gateway.Common.Guidewire.OptumAPI;

public partial class OptumAPIPortTypeClient
{
    private readonly ILogger<OptumAPIPortTypeClient> _logger;

    public OptumAPIPortTypeClient(string endpointUrl, ILoggerFactory loggerFactory) :
        base(GetBindingForEndpoint(endpointUrl), GetEndpointAddress(endpointUrl))
    {
        _logger = loggerFactory.CreateLogger<OptumAPIPortTypeClient>();
        ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
            new X509ServiceCertificateAuthentication
            {
                CertificateValidationMode = X509CertificateValidationMode.None,
                RevocationMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck
            };

        ConfigureEndpoint(this.Endpoint, this.ClientCredentials);

        this.Endpoint.EndpointBehaviors.Add(
            new LoggingEndpointBehaviour(
                new LoggingClientMessageInspector(loggerFactory.CreateLogger<LoggingClientMessageInspector>())));
    }

    private static Binding GetBindingForEndpoint(string endpointUrl)
    {
        bool isHttps = endpointUrl.StartsWith("https://");

        if (isHttps)
        {
            CustomBinding result = new CustomBinding();
            TextMessageEncodingBindingElement textBindingElement = new TextMessageEncodingBindingElement();
            textBindingElement.MessageVersion = MessageVersion.CreateVersion(EnvelopeVersion.Soap11, AddressingVersion.None);
            result.Elements.Add(textBindingElement);

            HttpsTransportBindingElement httpBindingElement = new HttpsTransportBindingElement
            {
                AllowCookies = true,
                MaxBufferSize = int.MaxValue,
                MaxReceivedMessageSize = int.MaxValue
            };
            result.Elements.Add(httpBindingElement);

            return result;
        }
        else
        {
            CustomBinding result = new CustomBinding();
            TextMessageEncodingBindingElement textBindingElement = new TextMessageEncodingBindingElement();
            textBindingElement.MessageVersion = MessageVersion.CreateVersion(EnvelopeVersion.Soap11, AddressingVersion.None);
            result.Elements.Add(textBindingElement);

            HttpTransportBindingElement httpBindingElement = new HttpTransportBindingElement
            {
                AllowCookies = true,
                MaxBufferSize = int.MaxValue,
                MaxReceivedMessageSize = int.MaxValue
            };
            result.Elements.Add(httpBindingElement);
            return result;
        }
    }

    private static EndpointAddress GetEndpointAddress(string endpointUrl)
    {
        return new EndpointAddress(endpointUrl);
    }

    public async Task CloseOrAbortServiceChannel()
    {
        bool isClosed = false;

        if (State == CommunicationState.Closed)
        {
            return;
        }

        try
        {
            // if the state is not faulted, try to close the connection
            // NOTE: We'll handle any expected exceptions below so they're not  propagated farther
            if (State != CommunicationState.Faulted)
            {
                await CloseAsync().ConfigureAwait(false);
                isClosed = true;
            }
        }
        catch (CommunicationException communicationException)
        {
            _logger?.LogError(communicationException, $"{nameof(Close)} - Communication error");
        }
        catch (TimeoutException timeoutException)
        {
            _logger?.LogError(timeoutException, $"{nameof(Close)} - Timed out");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"{nameof(Close)} - Unexpected error encountered");

            // An unexpected exception that we don't know how to handle.  This one we *do* need to bubble up
            throw;
        }
        finally
        {
            // If the state was "Faulted" or any exception occurred while doing the Close(), then we need to Abort()
            if (!isClosed)
            {
                AbortServiceChannel();
            }
        }
    }

    private void AbortServiceChannel()
    {
        try
        {
            Abort();
        }
        catch (Exception ex)
        {
            // An unexpected exception that we don't know how to handle.
            // - we should NOT retry the Abort() because it has already failed and there is nothing to suggest it could be successful next time
            // - the abort may have partially succeeded
            // - the actual service call may have been successful
            //
            // The only thing we can do is hope that the channel's resources have been released.
            // We don't rethrow this exception because the actual service operation call might have succeeded
            // and an exception closing the channel should not stop the client from doing whatever it does next.
            //
            _logger?.LogError(ex, $"{nameof(Abort)} - Unexpected error encountered");
        }
    }
}
