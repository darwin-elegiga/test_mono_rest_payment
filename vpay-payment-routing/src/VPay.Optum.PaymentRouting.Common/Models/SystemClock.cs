using System;
using VPay.Optum.PaymentRouting.Common.Interfaces;

namespace VPay.Optum.PaymentRouting.Common.Models
{
    public class SystemClock : ISystemClock
    {
        public DateTime Now => DateTime.Now;
    }
}
