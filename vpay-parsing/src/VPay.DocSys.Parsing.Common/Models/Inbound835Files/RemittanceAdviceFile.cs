using System;
using System.Collections.Generic;
using System.Text;

namespace VPay.DocSys.Parsing.Common.Inbound835Files
{
    /// <summary>
    /// Represents a inbound 835 remittance advice file.
    /// </summary>
    public class RemittanceAdviceFile
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
        /// Gets the TransactionId associated with the file.
        /// </summary>
        public long TransactionId { get; set; }

        /// <summary>
        /// Gets the client code associated with the file.
        /// </summary>
        public string ClientCode { get; set; }

        /// <summary>
        /// Gets or time stamp associated with the time stamp.
        /// </summary>
        public string TimeStamp { get; set; }

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
            return $"{GetType().Name} {{ FileName={FileName} }}";
        }
    }
}
