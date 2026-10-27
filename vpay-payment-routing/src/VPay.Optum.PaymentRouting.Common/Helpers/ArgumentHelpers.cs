using System;

namespace VPay.Optum.PaymentRouting.Common.Helpers
{
    public static class ArgumentHelpers
    {
        public static class ThrowIf
        {
            public static void IsNull<T>(T value, string parameterName)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(parameterName);
                }
            }
        }
    }
}
