using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using DS = VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Common.Edi835
{
    public static class Edi835Extension
    {
        /// <summary>
        /// Parsing and the RedCard process create the .docs file with a temporary name
        /// in a known location to be picked up later to be included in the 835 tar.
        /// </summary>
        /// <param name="edi835">The Edi835 record with the minimal information to generate the filename.</param>
        /// <param name="extension">Extension to add, if desired, with or without leading '.'</param>
        /// <returns>The expected filename that should have been either parsed or returned by RedCard for the docs file.</returns>
        public static string GetTempFilename(this DS.Edi835 edi835, string extension = "")
        {
            var ext = (string.IsNullOrWhiteSpace(extension) ? "" :
                (extension.StartsWith(".") ? "" : ".") + extension);

            return $"{edi835.TaxId.Trim()}_{edi835.OriginCode.Trim()}_{edi835.ClientCode.Trim()}_" +
                $"{edi835.BillingEntityCode.Trim()}_{edi835.UserKey.Trim().Replace(' ', '-')}_" +
                    $"{edi835.TransactionId}_DD_0000000000{ext}";
        }

        /// <summary>
        /// This is the final filename of each item in the 835 package (pdf, docs, trn, payfile and the tar itself)
        /// </summary>
        /// <param name="e">The Edi835 record with the minimal information to generate the filename.</param>
        /// <param name="extension">Extension to add, if desired, with or without leading '.'</param>
        /// <returns>A deterministic filename for all items within an 835 package that should be unique for a given instance of an 835 package.</returns>
        public static string GetFilename(this DS.Edi835 e, string extension = "")
        {
            var ext = (string.IsNullOrWhiteSpace(extension) ? "" :
                (extension.StartsWith(".") ? "" : ".") + extension);

            return  $"{e.TaxId.Trim()}_" +
                    $"{e.OriginCode.Trim()}_" +
                    $"{e.ClientCode.Trim()}_" +
                    $"{e.BillingEntityCode.Trim()}_" +
                    $"{e.UserKey.Trim().Replace(' ', '-')}_" +
                    $"{e.TransactionId}_" +
                    $"{( e.IsReissue ? "RE" : "NEW")}_" +
                    $"{e.CreatedDateTime:yyyyMMddHHmmss}" +
                    $"{ext}";
        }


        /// <summary>
        /// For a given Edi835, determine the folder where the items should be located.
        /// </summary>
        /// <param name="edi835">The Edi835 record with the minimal information to determine which folder docs should be in.</param>
        /// <param name="rootFolder">Root folder where docs files are stored.</param>
        /// <returns>folder where the items should be located</returns>
        public static string GetExpectedDocsFolder(this DS.Edi835 edi835, string rootFolder)
        {
            if ( edi835.OriginCode.Equals("RC", StringComparison.OrdinalIgnoreCase))
            {
                // RedCard dumps all the files in a single folder. Not the ideal situation... but here we are.
                return rootFolder;
            }
            // Should be something like B123456789_FS987654321.sts
            var fn = Path.GetFileNameWithoutExtension(edi835.PaymentStatusFile);
            if (string.IsNullOrWhiteSpace(fn) || fn.IndexOf('_') < 0)
            {
                throw new ArgumentException("PaymentStatusFile must exist and contain '_'");
            }
            var parts = fn.Split("_", StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                throw new ArgumentException("PaymentStatusFile must have iSeries filename component.");
            }

            return Path.Combine(rootFolder, edi835.ClientCode, edi835.BillingEntityCode, parts[1]);
        }

        /// <summary>
        /// Determines if an Edi835 is eligible to be QueuedForGeneration. A message that has already been queued for
        /// generation can be queued again, up until it had been updated in the database
        /// </summary>
        /// <param name="edi835"></param>
        /// <returns></returns>
        public static bool CanBeQueuedForGeneration(this DS.Edi835 edi835)
        {

            return !(edi835?.IsTerminalStatus ?? true) &&
                (edi835.StatusCode == DS.Edi835StatusCode.Initiated || edi835.StatusCode == DS.Edi835StatusCode.QueuedForGeneration);
           

        }

    }
}
