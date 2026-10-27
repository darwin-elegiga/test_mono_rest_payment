using System;
using VPay.DocSys.Data;

namespace VPay.Parsing.Common
{
    public static class StpPaymentExtensions
    {
        public static bool IsB2B(this StpPayment stpPayment)
        {
            ArgumentNullException.ThrowIfNull(stpPayment);

            return EnrollTypes.IsB2B(stpPayment.EnrollType);
        }

        public static bool IsC2B(this StpPayment stpPayment)
        {
            ArgumentNullException.ThrowIfNull(stpPayment);

            return EnrollTypes.IsC2B(stpPayment.EnrollType);
        }

        public static bool IsSDS(this StpPayment stpPayment)
        {
            ArgumentNullException.ThrowIfNull(stpPayment);
            return Edi835Types.IsSDS(stpPayment.Edi835Type);
        }

        public static bool IsPRS(this StpPayment stpPayment)
        {
            ArgumentNullException.ThrowIfNull(stpPayment);
            return Edi835Types.IsPRS(stpPayment.Edi835Type);
        }
    }
}
