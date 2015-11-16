using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Helpers.Linq
{
    public static class NodeTraverse
    {

        public static IEnumerable<T> Traverse<T>(this IEnumerable<T> items, Func<T, IEnumerable<T>> childSelector)
        {
            var stack = new Stack<T>(items);
            while(stack.Any())
            {
                var next = stack.Pop();
                yield return next;
                foreach (var child in childSelector(next))
                    stack.Push(child);
            }
        }

        public static IEnumerable<T> Traverse<T>(this T item, Func<T, IEnumerable<T>> childSelector)
        {
            return (new T[] { item }).Traverse<T>(childSelector);
        }
    }
}
