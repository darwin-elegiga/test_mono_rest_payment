using System;
using System.Collections.Generic;
using System.Linq;

using VPay.DocSys.Storage;
using VPay.EventBus.Messages.DocSys;

using FileDetailType = VPay.EventBus.Messages.DocSys.FileDetailType;

namespace VPay.Distribution.Common.Helpers
{
    public static class EventBusExtensions
    {
        public static IEnumerable<FileInformation> MapToFileInformation(this IEnumerable<IArchiveDetail> archiveDetails) => archiveDetails.Select(MapToFileInformation);

        public static FileInformation MapToFileInformation(this IArchiveDetail archiveDetail)
        {
            if (archiveDetail is null) { return null; }

            return new FileInformation
            {
                ArchiveDetailId = archiveDetail.ArchiveDetailId,
                Description = archiveDetail.Description,
                FileDetailType = MapFileDetailType(archiveDetail.FileDetailType),
                FileName = archiveDetail.FileName,
            };
        }

        public static FileDetailType MapFileDetailType(DocSys.Common.FileDetailType fdt) => (FileDetailType)Enum.Parse(typeof(FileDetailType), fdt.ToString(), true);
    }
}
