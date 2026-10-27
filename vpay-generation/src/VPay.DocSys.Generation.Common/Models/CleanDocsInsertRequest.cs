namespace VPay.DocSys.Generation.Common
{
    public class CleanDocsInsertRequest
    {
        /*
        /// <param name="statusFilePath"></param>
        /// <param name="csvTransactionIds">Em</param>
        */


           /// <summary>
           /// Full path to STS file
           /// </summary>
        public string StatusFilePath { get; set;  }
        /// <summary>
        /// Empty string for all transactions, otherwise, a comma separated string, listing each transactionId you want to delete, with no spaces. MUST match regex /^[0-9,]+$/
        /// </summary>
        public string CsvTransactionIds { get; set; }
        public string ApiKey { get; set; }

    }
}
