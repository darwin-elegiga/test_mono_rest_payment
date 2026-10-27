using System;
using System.Collections.Generic;
using System.Text;

namespace VPay.DocSys.Parsing.Common.Inbound835Files
{
    /// <summary>
    /// Represents a inbound 835 payment posted file.
    /// </summary>
    public class PaymentPostedFile
    {
        /// <summary>
        /// Represents a line in an <see cref="Inbound835Files.PaymentPostedFile"/>
        /// </summary>
        public class Line
        {
            public Line(long transactionId, string rejectionCode)
            {
                TransactionId = transactionId;
                RejectionCode = rejectionCode;
            }

            public long TransactionId { get; set; }

            public string RejectionCode { get; set; }

            public override string ToString()
            {
                return $"{GetType().Name} {{ TransactionId={TransactionId}; RejectionCode={RejectionCode}; }}";
            }
        }

        /// <summary>
        /// Gets or sets the path to directory in which the file exists.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the full path and name of the file.
        /// </summary>
        public string FullFileName { get; set; }

        /// <summary>
        /// Gets or sets the list of lines contains in the file.
        /// </summary>
        public IList<Line> Lines { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{GetType().Name} {{ FileName={FileName}; Lines={Lines.Count} }}";
        }
    }
}
