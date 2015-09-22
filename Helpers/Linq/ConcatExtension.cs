using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Helpers.Linq
{
    /// <summary>
    /// Concatination extension for LINQ helper
    /// </summary>
    public static class ConcatExtension
    {
        /// <summary>
        /// Get concatinated string from selector
        /// </summary>
        /// <typeparam name="TSelect">Selector type</typeparam>
        /// <param name="selecter">Selector enumeration</param>
        /// <param name="resultSelector">Selector function</param>
        /// <param name="delimiter">Delimiter between strings (e.g. ';' or new string)</param>
        /// <returns>Concatinated string</returns>
        public static string Concat<TSelect>(this IEnumerable<TSelect> selecter, Func<TSelect, string> resultSelector, string delimiter = "")
        {
            var res = string.Empty;
            foreach (var str in selecter.Select(i => resultSelector(i)))
                res += (string.IsNullOrWhiteSpace(res) ? string.Empty : delimiter) + str;
            return res;
        }
    }
}
