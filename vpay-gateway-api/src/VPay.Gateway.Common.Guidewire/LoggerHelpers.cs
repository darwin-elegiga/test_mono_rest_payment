using VPay.Gateway.Common.Guidewire.Models;

namespace VPay.Gateway.Common.Guidewire;

public static partial class LoggerHelpers
{
    public static void LogGuidewireResult(this ILogger logger, GuidewireResult result, string previousStatus)
    {
        if (result == null)
        {
            return;
        }

        using var paymentScope = logger.BeginScope(new Dictionary<string, object>
        {
            ["LastSentStatus"] = previousStatus
        });

        LogGuidewireStatus(
            logger,
            result.Status == "SUCCESS" ? LogLevel.Information : LogLevel.Error,
            result.Status,
            result.Message);
    }

    [LoggerMessage(
        EventId = 0,
        Message = "Payment Event result for status update is: {Result} - {ResultMessage}")]
    public static partial void LogGuidewireStatus(
        ILogger logger,
        LogLevel level, /* Dynamic log level as parameter, rather than defined in attribute. */
        string result,
        string resultMessage);
}
