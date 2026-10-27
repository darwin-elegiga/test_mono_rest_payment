namespace VPay.DocSys.Generation.Models
{
    public class CleanDocsServiceResponse
    {
        public int ExpectedArchiveCount { get; set; }
        public int ExpectedDetailCount { get; set; }
        public int DeletedFileCount { get; set; }
        public int DeletedArchiveCount { get; set; }
        public int DeletedDetailCount { get; set; }
        public int DeletedReissueTransactionCount { get; set; }
        public string Message { get; set; }
        public int DeletedDocumentDispositonRows { get; set; }
        public int DeletedDocumentTransactionRows { get; set; }
    }
}
