using System;
using System.IO;
using System.Linq;
using VPay.DocSys.Common;
using VPay.DocSys.Storage;
using DS=VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Common.Edi835
{
    public class FakePdfArchiveDetailStream : IArchiveDetailStream
    {
        public FakePdfArchiveDetailStream(DS.Edi835 edi)
        {
            this.FileName = edi.GetFilename("pdf");
        }


        public FakePdfArchiveDetailStream(Edi835CreationMessage creationMessage)
        {
            this.FileName = creationMessage.GetBaseFilename() + ".pdf";
        }

        public string ContentType
        {
            get
            {
                return "text/plain";
            }
        }
        public string FileName { get; set; }
        public string ZipFileName { get; set; }

        public long ArchiveDetailId { get; set; }
        public string Description { get; set; }
        public FileDetailType FileDetailType { get; set; }
        public long TransactionId { get; set; }

        public byte[] Data { get; set; }
        public long FileSize { get; set; }
        public int PageCount { get; set; }
        public DateTime CreateDateTime { get; set; }
        public string CreateLogin { get; set; }
        public DateTime ModifyDateTime { get; set; }
        public string ModifyLogin { get; set; }
        public long ArchiveFileId { get; set; }

        public void Dispose()
        {
            // Implemented in underlying DocSys.Storage class
            // But since this stream isn't kept open
        }

        public Stream Open()
        {
            return new MemoryStream("Fake PDF".Select(Convert.ToByte).ToArray()) as Stream;
        }


    }
}
