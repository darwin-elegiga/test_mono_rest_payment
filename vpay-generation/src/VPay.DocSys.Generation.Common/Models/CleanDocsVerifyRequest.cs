namespace VPay.DocSys.Generation.Common
{
    public class CleanDocsVerifyRequest
    {
        /// <summary>
        /// Clean docs Request Id 
        /// </summary>
        public int RequestId { get; set; }
        /// <summary>
        /// Number of archive records to delete.
        /// </summary>
        public int ArchivesToDelete { get; set; }
        /// <summary>
        /// Do you want to delete the distribution records (this will allow the document to be distributed again.)
        /// </summary>
        public bool DeleteDistribution { get; set; }
 

    }
}
