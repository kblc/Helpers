using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Helpers.Linq
{
    public static class ConcatExtension
    {
        public static string Concat<TSelect>(this IEnumerable<TSelect> selecter, Func<TSelect, string> resultSelector, string delimiter = "")
        {
            var res = string.Empty;
            foreach (var str in selecter.Select(i => resultSelector(i)))
                res += (string.IsNullOrWhiteSpace(res) ? string.Empty : delimiter) + str;
            return res;
        }
    }
}
