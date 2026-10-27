using System;
using System.Collections.Generic;
using System.Text;

namespace VPay.DocSys.Generation.Common.Edi835
{
    public class TarFileTransferSettings
    {
        public bool Enabled { get; set; } = false;
        public string Host { get; set; } = "";
        public int Port { get; set; } = 22;
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public string TransferPath { get; set; } = "";
    }
}
