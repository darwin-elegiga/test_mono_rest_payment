using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common.Edi835
{
    public class Edi835Package
    {
        public Edi835Package(Edi835CreationMessage message)
        {
            CreationMessage = message;
        }

        public Edi835CreationMessage CreationMessage { get; set; }

        public string DocsFileContent { get; set; }
        public string TrnFileContent { get; set; }
        public string PayfileFileContent { get; set; }

        protected Stream _pdfStream;

        public Stream Open()
        {
            _pdfStream?.Dispose();
            _pdfStream = new MemoryStream();
            string fakePdf = "Fake PDF";

            var writer = new StreamWriter(_pdfStream);
            writer.Write(fakePdf);
            writer.Flush();
            _pdfStream.Position = 0;
            return _pdfStream;
        }

        public void Dispose()
        {
            _pdfStream?.Dispose();
        }

    }
}
