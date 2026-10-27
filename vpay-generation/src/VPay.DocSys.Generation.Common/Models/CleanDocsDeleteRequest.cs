using System.Collections.Generic;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation.Common
{
    public class CleanDocsDeleteRequest
    {
        public IList<CleanDocsRequestResult> RequestedDeletions { get; set; }

        /// <summary>
        /// Do you also want to delete distribution data?
        /// </summary>
        public bool DeleteDistribution { get; set; }

        /// <summary>
        /// Remove the generated aux file if it exists (RC only)
        /// </summary>
        public bool RemoveAuxFile { get; set; }

 

    }
}
