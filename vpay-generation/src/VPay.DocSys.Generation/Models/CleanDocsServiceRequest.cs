using System;
using System.Collections.Generic;
using System.Text;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Models
{
    public class CleanDocsServiceRequest
    {
        public string FileName { get; set; }
        public bool IsReissue { get; set; }
        public string MasterArchiveSearch { get; set; }
        public StatusFile StatusFile { get; set; }
    }
}
