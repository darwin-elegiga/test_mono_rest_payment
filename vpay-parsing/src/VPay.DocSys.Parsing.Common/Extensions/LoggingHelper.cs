using System.Collections.Generic;

namespace VPay.DocSys.Parsing.Common.Extensions
{
    public static class LoggingHelper
    {
        public static int Sanitize(this int value)
        {
            // Define a maximum threshold for logging to prevent excessively large values from being logged.
            const int MaxLogValue = 1_000_000_000; // Example threshold, adjust as needed.

            // Ensure the value is non-negative and does not exceed the maximum allowed value for logging.
            if (value < -MaxLogValue)
            {
                return -MaxLogValue;
            }
            if (value > MaxLogValue)
            {
                return MaxLogValue;
            }
            return value;
        }

        public static IEnumerable<int> Sanitize(this IEnumerable<int> value)
        {
            if (value == null)
            {
                yield break;
            }   
            foreach (var item in value)
            {
                yield return item.Sanitize();
            }
        }

        public static long Sanitize(this long value)
        {
            // Define a maximum threshold for logging to prevent excessively large values from being logged.
            const long MaxLogValue = 1_000_000_000; // Example threshold, adjust as needed.

            // Ensure the value is non-negative and does not exceed the maximum allowed value for logging.
            if (value < -MaxLogValue)
            {
                return -MaxLogValue;
            }
            if (value > MaxLogValue)
            {
                return MaxLogValue;
            }
            return value;
        }

        public static IEnumerable<long> Sanitize(this IEnumerable<long> value)
        {
            if (value == null)
            {
                yield break;
            }
            foreach (var item in value)
            {
                yield return item.Sanitize();
            }
        }
    }
}
