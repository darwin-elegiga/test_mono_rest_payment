using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common.Inbound835Files
{
    /// <summary>
    /// Represents a inbound 835 acknowledgment file.
    /// </summary>
    public class AcknowledgmentFile
    {
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
        /// Gets the [PaymentId] value associated with the file.
        /// </summary>
        public string PaymentId { get; set; }

        /// <summary>
        /// Gets the [TransactionId] value associated with the file.
        /// </summary>
        public long TransactionId { get; set; }

        /// <summary>
        /// Gets the [Type] value associated with the file.
        /// </summary>
        public string TypeCode { get; set; }

        /// <summary>
        /// Gets the acknowledgment [TimeStamp] value associated with the file.
        /// </summary>
        public string AckTimeStamp { get; set; }

        /// <summary>
        /// Gets the [RejectionCode] value associated with the file.
        /// </summary>
        public string RejectionCode { get; set; }

        /// <summary>
        /// Gets the text contents of the file.
        /// </summary>
        public string Contents { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{GetType().Name} {{ {Contents} }}";
        }
    }
}
