using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Edi835;
using VPay.DocSys.Storage;
using DS=VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Edi835
{

    public interface IEdi835CreationService
    {
        /// <summary>
        /// Create a single .docs from a given Edi835CreationMessage
        /// </summary>
        /// <param name="message">This message is creating in parsing with all the needed
        /// data to create all three Edi835 documents.</param>
        /// <returns>The generated file content with the appropriate filename.</returns>
        Task<Edi835File> GenerateDocsFile(DS.Edi835 message);

        /// <summary>
        /// Create a single .payfile from a given Edi835CreationMessage
        /// </summary>
        /// <param name="message">This message is creating in parsing with all the needed
        /// data to create all three Edi835 documents.</param>
        /// <returns>The generated file content with the appropriate filename.</returns>
        Task<Edi835File> GeneratePayfileFile(DS.Edi835 message);

        /// <summary>
        /// Create a single .trn file from a given Edi835CreationMessage
        /// </summary>
        /// <param name="message">This message is creating in parsing with all the needed
        /// data to create all three Edi835 documents.</param>
        /// <returns>The generated file content with the appropriate filename.</returns>
        Task<Edi835File> GenerateTrnFile(DS.Edi835 message);

        /// <summary>
        /// Attempt to locate and return the appropriate .pdf file.
        /// If the file does not exist, throw an error in Prod.  In
        /// NON-Prod, the legacy default is to generate a text file
        /// with the text "Fake PDF" unless the "FetchPdfInNonProd"
        /// flag is set to true. In that case, also throw an error
        /// if the file is not found.
        /// </summary>
        /// <returns>.pdf file according to the internal algorithm</returns>
        /// <exception  cref="Common.Edi835.PdfFileNotFoundException">Unable to locate the file</exception>
        Task<IArchiveDetailStream> FetchPdfFor(DS.Edi835 message);

        /// <summary>
        /// Generates all of the Edi835 types for a given message (payment)
        /// and creates a single tarball (.tar file) for them.
        /// </summary>
        /// <param name="message">Information needed for generation and retrieval of the appropriate 835 documents.</param>
        /// <returns>The Edi835Files that comprise a complete package for a payment in a single tarball.</returns>
        Task<TarPackage> GenerateTarPackage(DS.Edi835 message);

        /// <summary>
        /// Generates all of the Edi835 types for a given message (payment)
        /// as memory streams.  They can then be consumed as needed by the caller.
        /// </summary>
        /// <param name="message">Information needed for generation and retrieval of the appropriate 835 documents.</param>
        /// <returns>An enumeration of the Edi835Files that comprise a complete package for a payment.</returns>
        Task<IEnumerable<Edi835File>> GeneratePackage(DS.Edi835 message);
    }
}
