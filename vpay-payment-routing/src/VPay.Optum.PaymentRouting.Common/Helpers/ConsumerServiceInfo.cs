using System;

namespace VPay.Optum.PaymentRouting.Common.Helpers
{
    public class ConsumerServiceInfo : IUserInfo
    {
        public string GetCurrentUserName()
        {
            return Environment.MachineName;
        }
    }
}
