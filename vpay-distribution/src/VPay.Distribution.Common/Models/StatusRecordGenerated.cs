using System;
using System.Linq;
using VPay.Distribution.Common.Helpers;
using VPay.DocSys.Common;
using VPay.DocSys.Storage;

namespace VPay.Distribution.Common
{
    public class StatusRecordGenerated : StatusRecord
    {

        public ArchiveFile ArchiveFile { get; set; }

        public long TransactionId => Convert.ToInt64(TXID.RemoveLeadingZeroes());


        public ArchiveDetail GetPacketArchive()
        {
            if (ArchiveFile == null || string.IsNullOrWhiteSpace(PAYTYPE))
            {
                return null;
            }

            var fileNameExtension = PayTypeExtensions.ToFileExtension(PAYTYPE);

            var result = ArchiveFile.ArchiveDetails.Cast<ArchiveDetail>()
                .OrderByDescending(x => x.ArchiveDetailId)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.FileName) && x.FileName.EndsWith(fileNameExtension, StringComparison.OrdinalIgnoreCase));
            return result;
        }

    }
}
