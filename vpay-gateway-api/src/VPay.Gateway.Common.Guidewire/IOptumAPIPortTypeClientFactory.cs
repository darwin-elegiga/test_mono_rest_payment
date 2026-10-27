namespace VPay.Gateway.Common.Guidewire.OptumAPI;

public interface IOptumAPIPortTypeClientFactory
{
    OptumAPIPortTypeClient CreateClient(string endpointUrl);
}
