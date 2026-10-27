namespace VPay.Optum.Nc835FileProcessor.Application.Models
{
    public class Nc835FileSettings
    {
        /// <summary>
        /// Enrollment files base folder
        /// used in the health check
        /// </summary>
        public string BaseFolderPath { get; set; } = "";

        public string RecordEncryptionKey { get; set; } = "";

        public int PrefetchCount { get; set; } = 1;
    }
}
