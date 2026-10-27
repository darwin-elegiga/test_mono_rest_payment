using VPay.DocSys.Common;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation
{
    public class GeneratedFile : IGeneratedFile
    {
        public string TransactionId { get; set; }
        public string FileName { get; set; }
        public FileDetailType FileDetailType { get; set; }
        public string Description { get; set; }
        public int PageCount { get; set; }
    }
}
