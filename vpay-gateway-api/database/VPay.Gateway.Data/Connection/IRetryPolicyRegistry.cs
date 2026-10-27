using Polly;

namespace VPay.Gateway.Data.Connection;

public interface IRetryPolicyRegistry
{
    IAsyncPolicy GetStandardRetryPolicy();

    IAsyncPolicy GetStandardSaveQueryRetryPolicy();

    IAsyncPolicy GetOpenConnectionRetryPolicy();
}
