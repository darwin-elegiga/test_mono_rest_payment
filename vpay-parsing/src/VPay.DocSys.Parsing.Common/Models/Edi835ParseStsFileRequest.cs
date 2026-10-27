using System.Collections.Generic;
using System.ComponentModel;

namespace VPay.DocSys.Parsing.Common
{
    public class Edi835ParseStsFileRequest
    {
        /// <summary>
        /// Fully qualified path on the server to the sts file (usually /SRVFS/bogner...)
        /// </summary>
        public string StsFilepath { get; set; }

        /// <summary>
        /// Add Edi835Status for "QueuedForGeneration" and place on the queue designated in the appsettings.
        /// </summary>
        public bool QueueForGeneration { get; set; }
    }
}
