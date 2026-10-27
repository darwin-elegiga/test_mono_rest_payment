using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VPay.DocSys.Generation.Common.Edi835
{
    public class TarPackage
    {
        public string ContentType => "application/x-tar";
        public string Filename { get; set; }
        public Stream Stream { get; set; }
    }
}
