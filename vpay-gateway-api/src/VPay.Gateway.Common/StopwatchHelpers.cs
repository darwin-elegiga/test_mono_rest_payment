using System.Diagnostics;

namespace VPay.Gateway.Common;

public static class StopwatchHelpers
{
    public static long GetTimestamp()
    {
        return Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Will convert then find the difference between start time and the current time in milliseconds
    /// </summary>
    /// <remarks>
    /// Start is in the number of ticks. To convert to milliseconds, you must divide by <see cref="Stopwatch.Frequency"/>
    /// </remarks>
    /// <param name="start">Start time in Ticks</param>
    public static double GetElapsedMilliseconds(this long start)
    {
        return (GetTimestamp() - start) * 1000 / (double)Stopwatch.Frequency;
    }
}
