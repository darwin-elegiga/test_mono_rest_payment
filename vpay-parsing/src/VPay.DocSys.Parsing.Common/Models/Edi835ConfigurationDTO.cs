using System;
using System.Collections.Generic;
using System.Text;

namespace VPay.DocSys.Parsing.Common
{
    public class Edi835ConfigurationDTO
    {
        public string ClientCode { get; set; }
        public bool IsActive { get; set; }
        public bool IsInactiveButGenerate { get; set; }
    }
}
