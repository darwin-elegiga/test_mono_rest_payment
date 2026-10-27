using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VPay.DocSys.Parsing.Common.Extensions
{
    public static class EnumHelpers
    {
        /// <summary>
        /// Will create an end point dictionary using the values from <paramref name="T"/> and the values from <paramref name="endPointFragmentDictionary"/>
        /// <paramref name="T"/> must be an Enum
        /// <paramref name="endPointFragmentDictionary"/> keys must contain all values from <paramref name="T"/>
        /// </summary>
        /// <typeparam name="T">Enum name</typeparam>
        /// <param name="endPointFragmentDictionary">dictionary containing keys that match all the <paramref name="T"/> values</param>
        /// <returns>Dictionary<T,string> where the values are the values from the <paramref name="endPointFragmentDictionary"/></returns>
        public static Dictionary<T, string> GetEndPointDictionary<T>(IReadOnlyDictionary<string,string> endPointFragmentDictionary)
            where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            return Enum.GetValues(typeof(T))
                .Cast<T>()
                .ToDictionary(
                    k => k,
                    v => endPointFragmentDictionary[v.ToString(CultureInfo.InvariantCulture)]);
        }
    }
}
