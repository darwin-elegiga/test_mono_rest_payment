using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common.Interfaces;

namespace VPay.DocSys.Parsing.Common
{
    public class PdfService : IPdfService
    {
        public PdfService()
        {
        }

        public bool EndsWithEof(byte[] bytes)
        {
            using (var m = new MemoryStream(bytes, false))
            using (var sr = new StreamReader(m))
            {
                return sr.ReadToEnd().EndsWith("%%EOF\r\n");
            }
        }
    }
}
