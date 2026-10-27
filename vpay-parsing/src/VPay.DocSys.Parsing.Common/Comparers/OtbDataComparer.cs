using System;
using System.Collections.Generic;
using System.Linq;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common.Comparers
{
    /// <summary>
    /// Provides comparison functionality for OTB data.
    /// </summary>
    public class OtbDataComparer
    {
        /// <summary>
        /// Compares the list of fields for a given billing entity extracted from the OTB API
        /// and the list of fields for the same billing entity extracted from the OTB File and
        /// checks if the field names match and if the field values for each field name also
        /// match between them.
        /// </summary>
        /// <param name="beCode">The billing entity code of the fields to compare.</param>
        /// <param name="otbApiData">The dictionary of billing entities extracted from the OTB API
        /// having the billing entity code as the key and having each dictionary of billing entity fields
        /// as the value where each dictionary of billing entity fields contains the field names as the
        /// key and field values as the value.</param>
        /// <returns>The list of discrepancy messages from the differences resulting from the
        /// <param name="otbFileData">The dictionary of billing entities extracted from the OTB File
        /// having the billing entity code as the key and having each dictionary of billing entity fields
        /// as the value where each dictionary of billing entity fields contains the field names as the
        /// key and field values as the value.</param>
        /// <returns>The list of discrepancy messages from the differences resulting from the
        /// comparison.</returns>
        public static IEnumerable<string> CompareOtbData(
            string beCode,
            IDictionary<string, IDictionary<string, string>> otbApiData,
            IDictionary<string, IDictionary<string, string>> otbFileData)
        {
            ArgumentHelpers.ThrowIf.IsNullOrWhiteSpace(beCode, nameof(beCode));
            ArgumentHelpers.ThrowIf.IsNull(otbApiData, nameof(otbApiData));
            ArgumentHelpers.ThrowIf.IsNull(otbFileData, nameof(otbFileData));

            var discrepancies = new List<string>();

            if (otbApiData.ContainsKey(beCode) && otbFileData.ContainsKey(beCode))
            {
                var fromApi = otbApiData[beCode];
                var fromFile = otbFileData[beCode];

                var onApiOnly = fromApi.Keys.Except(fromFile.Keys);
                var onFileOnly = fromFile.Keys.Except(fromApi.Keys);
                var onBoth = fromFile.Keys.Intersect(fromApi.Keys);
                var onApi = fromApi.Where(x => onBoth.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
                var onFile = fromFile.Where(x => onBoth.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
                var diffInValue = onApi.Where(x => onFile[x.Key] != x.Value).ToDictionary(x => x.Key, x => x.Value);

                foreach (var fieldName in onApiOnly)
                {
                    discrepancies.Add($"BE '{beCode}' Field '{fieldName}' from API was not found in OTB File.");
                }

                foreach (var fieldName in onFileOnly)
                {
                    discrepancies.Add($"BE '{beCode}' Field '{fieldName}' from OTB File was not found in API.");
                }

                foreach (var field in diffInValue)
                {
                    if (field.Key == "RawBEName" || field.Key.Contains("Account"))  // Might contain account information which should not be logged.
                    {
                        discrepancies.Add($"BE '{beCode}' Field '{field.Key}' from API has a different value in OTB File.");
                    }
                    else
                    {
                        discrepancies.Add($"BE '{beCode}' Field '{field.Key}' from API ('{field.Value}') differs from OTB File ('{onFile[field.Key]}').");
                    }
                }
            }
            else
            {
                if (!otbApiData.ContainsKey(beCode))
                {
                    discrepancies.Add($"BE '{beCode}' from File was not found in OTB API.");
                }
                if (!otbFileData.ContainsKey(beCode))
                {
                    discrepancies.Add($"BE '{beCode}' from API was not found in OTB File.");
                }
            }

            return discrepancies.Distinct();
        }
    }
}
