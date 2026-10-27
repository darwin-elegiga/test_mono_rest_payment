using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation.Common.Helpers
{
    public static class RegexHelper
    {
        /// <summary>
        /// Returns the value of the last successful <see cref="Group"/>
        /// in one <see cref="Match"/>s <see cref="GroupCollection"/>
        /// or null otherwise
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        public static string GetLastSuccess(this Match match)
        {
            ThrowIf.IsNull(match, nameof(match));

            if (match.Groups.Count == 0) { return null; }

            var last = (match.Groups as IEnumerable<Group>)
                .LastOrDefault(_ => _.Success);

            return last?.Value;
        }

        /// <summary>
        /// Returns the value of the last successful <see cref="Group"/>
        /// in the last successful <see cref="Match"/> for <see cref="MatchCollection"/>'s <see cref="GroupCollection"/>s
        /// or null otherwise
        /// </summary>
        /// <param name="matchCollection"></param>
        /// <returns></returns>
        public static string GetLastSuccess(this MatchCollection matchCollection)
        {
            ThrowIf.IsNull(matchCollection, nameof(matchCollection));

            if (matchCollection.Count == 0) { return null; }

            var last = (matchCollection as IEnumerable<Match>)
                .LastOrDefault(_ => _.Success)
                ?.GetLastSuccess();

            return last;
        }
    }
}
