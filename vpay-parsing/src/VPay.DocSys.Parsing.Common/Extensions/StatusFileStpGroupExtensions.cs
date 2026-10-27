using System;
using VPay.DocSys.Data;

namespace VPay.DocSys.Parsing.Common.Extensions
{
    public static class StatusFileStpGroupExtensions
    {
        public static bool IsB2B(this StatusFileStpGroup stpGroup)
        {
            ArgumentNullException.ThrowIfNull(stpGroup);

            return EnrollTypes.IsB2B(stpGroup.EnrollType);
        }

        public static bool IsC2B(this StatusFileStpGroup stpGroup)
        {
            ArgumentNullException.ThrowIfNull(stpGroup);

            return EnrollTypes.IsC2B(stpGroup.EnrollType);
        }

        public static bool IsSDS(this StatusFileStpGroup stpGroup)
        {
            ArgumentNullException.ThrowIfNull(stpGroup);
            return Edi835Types.IsSDS(stpGroup.Edi835Type);
        }

        public static bool IsPRS(this StatusFileStpGroup stpGroup)
        {
            ArgumentNullException.ThrowIfNull(stpGroup);
            return Edi835Types.IsPRS(stpGroup.Edi835Type);
        }
    }
}
