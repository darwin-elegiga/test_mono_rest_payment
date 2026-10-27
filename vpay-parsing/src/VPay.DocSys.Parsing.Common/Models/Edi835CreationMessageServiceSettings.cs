using System;
using System.Collections.Generic;
using System.Text;

namespace VPay.DocSys.Parsing.Common
{
    public class Edi835CreationMessageServiceSettings
    {
        /// <summary>
        /// Root directory where client raw files are stored.
        /// </summary>
        public string UploadFilesPath { get; set; } = @"/SRVFS/bogner/uploadFiles";

        public string TransferRoot { get; set; }
    }
}
