namespace VPay.Gateway.Common.Guidewire.OptumAPI;

public class OptumAPIPortTypeClientFactory : IOptumAPIPortTypeClientFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public OptumAPIPortTypeClientFactory(ILoggerFactory loggerFactory) =>
        _loggerFactory = loggerFactory;

    public OptumAPIPortTypeClient CreateClient(string endpointUrl) =>
        new OptumAPIPortTypeClient(endpointUrl, _loggerFactory);
}
