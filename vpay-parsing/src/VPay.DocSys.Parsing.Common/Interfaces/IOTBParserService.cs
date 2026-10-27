using System;
using System.Collections.Generic;
using System.IO;

namespace VPay.DocSys.Parsing.Common.Interfaces
{
    /// <summary>
    /// This will be used to parse the otb file into a dictionary
    /// </summary>
    public interface IOTBParserService
    {
        /// <summary>
        /// This will open the otb file in read mode. It will go line by line and create a dictionary for each line.
        /// </summary>
        /// <param name="filePath">The path to the otb file</param>
        /// <param name="billingEntityKey">The header of the key you want to group on.</param>
        /// <returns>A grouped dictionary of key/value pairs by the <paramref name="billingEntityKey"/></returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> or <paramref name="billingEntityKey"/> is null</exception>
        /// <exception cref="ArgumentException">Thrown <paramref name="filePath"/> or <paramref name="billingEntityKey"/> is empty</exception>
        /// <exception cref="VPay.DocSys.Common.Exceptions.InvalidFileFormatException">Thrown when there is an error in parsing the orb file due to invalid formatting of the file</exception>
        IDictionary<string, IDictionary<string, string>> ParseOTBFile(string filePath, string billingEntityKey = "BillingEntity");

        /// <summary>
        /// This expects a streamreader to an otb file. It will go line by line and create a dictionary for each line.
        /// </summary>
        /// <param name="filePath">The path to the otb file</param>
        /// <param name="billingEntityKey">The header of the key you want to group on.</param>
        /// <returns>A grouped dictionary of key/value pairs by the <paramref name="billingEntityKey"/></returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="otbFileStream"/> or <paramref name="billingEntityKey"/> is null</exception>
        /// <exception cref="ArgumentException">Thrown <paramref name="otbFileStream"/> or <paramref name="billingEntityKey"/> is empty</exception>
        /// <exception cref="VPay.DocSys.Common.Exceptions.InvalidFileFormatException">Thrown when there is an error in parsing the orb file due to invalid formatting of the file</exception>
        IDictionary<string, IDictionary<string, string>> ParseOTBData(StreamReader otbFileStream, string billingEntityKey = "BillingEntity");
    }
}
