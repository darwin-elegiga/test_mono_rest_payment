using System;

namespace VPay.DocSys.Parsing.Common
{
    /// <summary>
    /// Contains information about an inbound 835 file to be processed.
    /// </summary>
    public class Inbound835FileMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Inbound835FileMessage"/> class.
        /// </summary>
        public Inbound835FileMessage()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Inbound835FileMessage"/> class.
        /// </summary>
        /// <param name="filePath">The file path to use.</param>
        public Inbound835FileMessage(string filePath)
        {
            FilePath = filePath;
        }

        /// <summary>
        /// Gets or sets the full path and name of the inbound 835 file.
        /// </summary>
        public string FilePath { get; set; }
    }
}
