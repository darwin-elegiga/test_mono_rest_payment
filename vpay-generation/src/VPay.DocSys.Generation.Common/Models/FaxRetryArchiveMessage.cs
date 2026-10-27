using System;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Used for creating an archive for fax retry documentation in VPay 360
    /// A stripped down version of the <code>FaxJob</code> class from <code>FaxManagement</code>
    /// </summary>
    public class FaxRetryArchiveMessage
    {
        /// <summary>
        /// Used in creating the archive
        /// </summary>
        public long CorrespondenceId { get; set; }

        /// <summary>
        /// Used in creating the archive
        /// </summary>
        public IList<long> TransactionIds { get; set; }

        /// <summary>
        /// Used in creating the archive
        /// </summary>
        public DateTime UpdateDateTime { get; set; }

        public string DocumentPath { get; set; }

        public int MaxRetries { get; set; }

        public int RetriesRemaining { get; set; }

        public string PhoneNumber { get; set; }

        /// <summary>
        /// Used in creating the archive
        /// </summary>
        public string TpaCode { get; set; }

        /// <summary>
        /// Get the actual retry count from <see cref="MaxRetries"/> and <see cref="RetriesRemaining"/>
        /// </summary>
        private int RetryCount => (MaxRetries - RetriesRemaining) + 1;

        /// <summary>
        /// Get the <see cref="RetryCount"/>/<see cref="MaxRetries"/> ratio string
        /// </summary>
        private string RetryRatio => $"{RetryCount,2} of {MaxRetries + 1,2}";

        public string Description => $"Fax Retry {RetryRatio} to {PhoneNumber}";

        public string GetFileContents(long transactionId)
        {
            return $"Fax Retry {RetryRatio} for Transaction Id {transactionId} with Correspondence Id {CorrespondenceId} using {PhoneNumber} on {UpdateDateTime:MMM.dd, yyyy @ hh:mm:ss} {UpdateDateTime.ToString("tt").ToLower()}";
        }

        /// <summary>
        /// replace .tiff ending with correspondence_id.fax_retry_#_of_#.txt
        /// </summary>
        public string GetRetryFileName(IFileSystem fileSystem)
        {
            return fileSystem.Path.ChangeExtension(
                fileSystem.Path.GetFileName(DocumentPath),
                $".{CorrespondenceId}.fax_retry_{RetryRatio.Replace(" ", "_")}.txt");
        }
    }
}
