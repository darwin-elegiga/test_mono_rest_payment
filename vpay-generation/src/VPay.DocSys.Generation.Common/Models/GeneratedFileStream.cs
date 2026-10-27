using System.IO;
using VPay.DocSys.Common;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation
{
    public class GeneratedFileStream : IGeneratedFileStream
    {
        public string TransactionId { get; set; }
        public string FileName { get; set; }
        public FileDetailType FileDetailType { get; set; }
        public string Description { get; set; }
        public int PageCount { get; set; }
        public Stream Stream { get; set; }
        public long Length { get; set; }
    }
}
