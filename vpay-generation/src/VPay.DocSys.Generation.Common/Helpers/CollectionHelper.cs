using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common.Helpers
{
    public static class CollectionHelper
    {
        /// <summary>
        /// Checks the given string collection for the string value
        /// uses uppercase and invariant culture
        /// </summary>
        /// <param name="collection">string collection</param>
        /// <param name="value">string value to check</param>
        /// <returns></returns>
        public static bool ContainsString(this ICollection<string> collection, string value)
        {
            ArgumentHelpers.ThrowIf.IsNull(collection, nameof(collection));
            ArgumentHelpers.ThrowIf.IsNull(value, nameof(value));

            return collection.Any(_ => _.ToUpper(CultureInfo.InvariantCulture).Contains(value.ToUpper(CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// Checks the given string collection for the string values
        /// using StringComparison.InvariantCultureIgnoreCase
        /// uses uppercase and invariant culture
        /// </summary>
        /// <param name="collection">string collection</param>
        /// <param name="values">multiple string values to check</param>
        /// <returns></returns>
        public static bool ContainsStrings(this ICollection<string> collection, params string[] values)
        {
            ArgumentHelpers.ThrowIf.IsNull(collection, nameof(collection));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(values, nameof(values));

            return values.Any(collection.ContainsString);
        }
    }
}
