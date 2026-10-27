using System.Collections.Generic;

namespace VPay.DocSys.Generation.Models
{
    public static class DictionaryExtensions
    {

        /// <summary>
        /// If the given key exists in the dictionary AND its value is a IDictionary&lt;string,dynamic&gt;, return the IDictionary&lt;string,dynamic&gt;
        /// otherwise, return null
        /// </summary>
        /// <remarks>I (Jeff Hill) created this class to remedy a situation where the code assumed that deeply nested
        /// dictionaries were dictionaries all the way down.  Due to some weirdness with EAG, there are multiple eob under eob_data,
        /// requiring it to be a List instead.  This lets you check that the keys exist and chains to the next dictionary.
        ///
        /// Use null coalescing to "run the tree down" to the leaf you're looking for.
        /// PROD-2413 Migrate EAG to the new Document System
        /// </remarks>
        /// <param name="myDictionary"></param>
        /// <param name="key"></param>
        /// <returns>returns IDictionary&lt;string,dynamic&gt;
        /// Use with null coalescing operator to see if key exists.</returns>
        public static IDictionary<string, dynamic> Key(this IDictionary<string, dynamic> myDictionary, string key)
        {
            if (myDictionary != null
                && myDictionary.ContainsKey(key)
                && myDictionary[key] != null
                && myDictionary[key] is IDictionary<string, dynamic>)
            {
                return myDictionary[key];
            }
            return null;
        }
    }
}
