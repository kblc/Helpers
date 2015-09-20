using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Helpers.Linq
{
    /// <summary>
    /// Generic comparer for linq Distinct() and other comparision operations
    /// </summary>
    /// <typeparam name="T">Comparable class type</typeparam>
    public class GenericCompare<T> : IEqualityComparer<T> where T : class
    {
        private Func<T, object> expr { get; set; }

        /// <summary>
        /// Create new instance
        /// </summary>
        /// <param name="expr">Comparision expression</param>
        public GenericCompare(Func<T, object> expr)
        {
            this.expr = expr;
        }

        /// <summary>
        /// IEqualityComparer Equals between two objects realisation.
        /// </summary>
        /// <param name="x">First object</param>
        /// <param name="y">Second object</param>
        /// <returns>True if equals</returns>
        public bool Equals(T x, T y)
        {
            var first = expr.Invoke(x);
            var sec = expr.Invoke(y);
            return (first != null && first.Equals(sec));
        }

        /// <summary>
        /// IEqualityComparer GetHashCode realisation 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }

        /// <summary>
        /// Get new instance
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static GenericCompare<T> Get(Func<T, object> expr)
        {
            return new GenericCompare<T>(expr);
        }
    }
}
