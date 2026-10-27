using System;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common
{
    public static class DocumentTransactionMessageExtension
    {
        public static bool IsEdi835Needed(this DocumentTransactionMessage message, Edi835ConfigurationCache cache)
        {
            return message?.StatusRecord?.IsEdi835Needed(cache) ?? false;
        }

        public static bool IsEdi835Needed(this StatusRecord record, Edi835ConfigurationCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache), "Edi835ConfigurationCache is null, cannot determine if Edi835 is required");
            }
            
            if (!string.IsNullOrWhiteSpace(record?.PORTALSTAT ?? ""))
            {
                return cache.IsEdi835Needed(record?.TPA, record?.ORIGIN).Result;
            }

            return false;
        }
    }
}
