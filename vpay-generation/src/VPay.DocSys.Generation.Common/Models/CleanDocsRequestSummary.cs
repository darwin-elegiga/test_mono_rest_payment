using System.Collections.Generic;
using System.Linq;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation.Common
{
    public class CleanDocsRequestSummary
    {
        public CleanDocsRequestSummary(IList<CleanDocsRequestResult> results)
        {
            RequestId = results?.FirstOrDefault()?.RequestId ?? 0;
            ArchivesToDelete = results?.Count(_ => _.ArchiveFileId > 0)  ?? 0;
        }

        public int RequestId { get; set; }
        public int ArchivesToDelete { get; set; }
        
    }
}
