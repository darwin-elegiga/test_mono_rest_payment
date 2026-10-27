using System.IO;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation.Common.Edi835
{
    public class PdfFile : Edi835File
    {
        public new byte[] Content { get; set; }

        public override Stream ContentAsStream()
        {
            return new MemoryStream(Content);
        }

        public void ReadArchiveDetailStream(IArchiveDetailStream archiveDetail)
        {
            using (var stream = archiveDetail.Open())
            using (var memStream = new MemoryStream())
            {
                stream.CopyTo(memStream);
                Content = memStream.ToArray();
            }
        }
    }
}
