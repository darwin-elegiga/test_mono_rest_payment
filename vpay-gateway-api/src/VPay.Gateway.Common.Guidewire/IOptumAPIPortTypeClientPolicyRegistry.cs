using Polly;

namespace VPay.Gateway.Common.Guidewire;

public interface IOptumAPIPortTypeClientPolicyRegistry
{
    /// <summary>
    /// Gets a retry policy to use when executing operations.
    /// </summary>
    /// <returns>The execution policy.</returns>
    IAsyncPolicy GetExecutionRetryPolicy();
}
