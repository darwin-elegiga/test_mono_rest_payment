using System.Collections.Generic;

namespace VPay.DocSys.Parsing.Common
{
    public class StatusFileSettings
    {
        public string EobMergeSettings { get; set; }
        public string EobMergeConfig { get; set; }
        public string PythonHome { get; set; }
        public string FileHome { get; set; }
        public string TarFolder { get; set; }
        public bool SaveMessages { get; set; }
        public bool EnableReIssue { get; set; }
    }
}
