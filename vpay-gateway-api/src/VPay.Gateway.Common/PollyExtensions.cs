using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Polly;

namespace VPay.Gateway.Common;

public static class PollyExtensions
{
    public static bool TryGetLogger(this Context context, [NotNullWhen(true)] out ILogger? logger)
    {
        if (context.TryGetValue(PolicyContextItems.Logger, out var loggerObject)
            && loggerObject is ILogger theLogger)
        {
            logger = theLogger;
            return true;
        }

        logger = null;
        return false;
    }
}
