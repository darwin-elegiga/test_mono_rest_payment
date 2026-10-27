using System;

namespace VPay.Optum.PaymentRouting.Common.Interfaces
{
    public interface ISystemClock
    {
        DateTime Now { get; }
    }
}
