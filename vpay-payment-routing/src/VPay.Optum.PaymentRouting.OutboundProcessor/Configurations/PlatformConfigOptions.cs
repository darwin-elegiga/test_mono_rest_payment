using System;
using System.Diagnostics.CodeAnalysis;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.Configurations
{
    [ExcludeFromCodeCoverage]
    public class PlatformConfigOptions
    {
        public const string ConfigName = "PlatformConfigApi";

        public Uri BaseAddress { get; set; } = null!;

        public TimeSpan? Timeout { get; set; }
    }
}
