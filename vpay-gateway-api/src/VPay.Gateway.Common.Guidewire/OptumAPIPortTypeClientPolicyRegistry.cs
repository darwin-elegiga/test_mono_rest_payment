using System.ServiceModel;
using Polly;
using Polly.Registry;

namespace VPay.Gateway.Common.Guidewire;

public class OptumAPIPortTypeClientPolicyRegistry : IOptumAPIPortTypeClientPolicyRegistry
{
    private readonly PolicyRegistry _registry = new PolicyRegistry();
    private readonly ILogger<OptumAPIPortTypeClientPolicyRegistry> _logger;

    private const string ExecutionRetryPolicy = "ExecutionRetryPolicy";

    public OptumAPIPortTypeClientPolicyRegistry(ILogger<OptumAPIPortTypeClientPolicyRegistry> logger) =>
        _logger = logger;

    public IAsyncPolicy GetExecutionRetryPolicy()
    {
        if (_registry.ContainsKey(ExecutionRetryPolicy))
        {
            return _registry.Get<IAsyncPolicy>(ExecutionRetryPolicy);
        }

        var waitAndRetryPolicy = Policy
            .Handle<CommunicationException>(x => x.Message.StartsWith("Connection refused", StringComparison.OrdinalIgnoreCase))
            .WaitAndRetryAsync(2,
                retryAttempt => TimeSpan.FromSeconds(5 + retryAttempt),
                onRetry: (exception, calculatedWaitDuration, retryCount, context) =>
                {
                    if (exception is CommunicationException communicationEx)
                    {
                        _logger.LogInformation(communicationEx, "{Operation} - Communication Error - Service Retry {RetryCount} occurred after {timeSpan}", context.OperationKey, retryCount, calculatedWaitDuration);
                    }
                });

        // using setter will use TryAdd behind the scenes and prevent duplicate key error
        _registry[ExecutionRetryPolicy] = waitAndRetryPolicy;

        return waitAndRetryPolicy;
    }
}
