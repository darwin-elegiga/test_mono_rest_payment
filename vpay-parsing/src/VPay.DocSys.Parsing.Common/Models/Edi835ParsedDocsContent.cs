using System;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common
{
    public class Edi835ParsedDocsContent
    {
        public Edi835ParsedDocsContent() {}
        public Edi835ParsedDocsContent(DocumentTransactionMessage message, string content)
        {
            DocumentTransactionMessage = message;
            Content = content;
        }

        /// <summary>
        /// This feature is used to pull all the EOB data for the .docs files from the
        /// inputFile in the tarData file.  Passing just this will allow us to separate
        /// the concerns so I can have an iterator write each one out into a folder where
        /// they can be available later for the distribution process to pick them
        /// up. 
        /// </summary>
        public DocumentTransactionMessage DocumentTransactionMessage { get; set; }

        /// <summary>
        /// The content of any single docs file should be "reasonable", especially where HEQTN or SWK are concerned.
        /// </summary>
        public string Content { get; set; }

        public long TransactionId
        {
            get
            {
                return Convert.ToInt64(DocumentTransactionMessage?.TransactionId.RemoveLeadingZeroes() ?? "0");
            }
        }

        public string TempFilename 
        {
            get
            {
                var r = DocumentTransactionMessage?.StatusRecord ?? (new StatusRecord());
                return $"{r.TIN.Trim()}_{r.ORIGIN.Trim()}_{r.TPA.Trim()}_{r.BILENT.Trim()}_{r.USERKEY.Trim().Replace(' ', '-')}_" +
                    $"{r.TXID.Trim().RemoveLeadingZeroes()}_DD_0000000000";
            }

        }

    }
}
