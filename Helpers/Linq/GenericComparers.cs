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
    public class GenericEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        private readonly Func<T, object> expr = null;
        private readonly bool withoutHash = false;

        /// <summary>
        /// Create new instance
        /// </summary>
        /// <param name="expr">Comparision expression</param>
        /// <param name="withoutHash">Disable hash function if true</param>
        public GenericEqualityComparer(Func<T, object> expr, bool withoutHash = false)
        {
            if (expr == null)
                throw new ArgumentNullException("expr");
            this.expr = expr;
            this.withoutHash = withoutHash;
        }

        /// <summary>
        /// IEqualityComparer Equals between two objects realisation.
        /// </summary>
        /// <param name="x">First object</param>
        /// <param name="y">Second object</param>
        /// <returns>True if equals</returns>
        public bool Equals(T x, T y)
        {
            try
            {
                var first = expr.Invoke(x);
                var sec = expr.Invoke(y);
                return ((first != null && first.Equals(sec)) || (sec != null && sec.Equals(first)));
            }
            catch(Exception ex)
            {
                var e = new Exception("Exception on GenericEqualityComparer.Equals(). See inner exception and data for details.", ex);
                e.Data.Add("X", x);
                e.Data.Add("Y", y);
                throw e;
            }
        }

        /// <summary>
        /// IEqualityComparer GetHashCode realisation 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(T obj)
        {
            if (withoutHash)
                return 0;
            return obj.GetHashCode();
        }

        /// <summary>
        /// Get new instance
        /// </summary>
        /// <param name="expr">Comparision expression</param>
        /// <param name="withoutHash">Disable hash function if true</param>
        /// <returns>New GenericEqualityComparer instance</returns>
        public static GenericEqualityComparer<T> Get(Func<T, object> expr, bool withoutHash = false)
        {
            return new GenericEqualityComparer<T>(expr, withoutHash);
        }
    }

    /// <summary>
    /// Generic comparer for linq OrderBy() and other comparision operations
    /// </summary>
    /// <typeparam name="T">Comparable class type</typeparam>
    public class GenericComperer<T> : IComparer<T> where T : class
    {
        private Func<T,IComparable> expr { get; set; }

        /// <summary>
        /// Create new instance
        /// </summary>
        /// <param name="expr">Comparision expression</param>
        public GenericComperer(Func<T, IComparable> expr)
        {
            if (expr == null)
                throw new ArgumentNullException("expr");
            this.expr = expr;
        }

        /// <summary>
        /// IComparer Compare between two objects realisation.
        /// </summary>
        /// <param name="x">First object</param>
        /// <param name="y">Second object</param>
        /// <returns>Comparision result</returns>
        public int Compare(T x, T y)
        {
            try
            { 
                var first = expr.Invoke(x);
                var sec = expr.Invoke(y);
                return (first != null ? first.CompareTo(sec) : (sec != null ? sec.CompareTo(first) : int.MaxValue));
            }
            catch(Exception ex)
            {
                var e = new Exception("Exception on GenericComperer.Compare(). See inner exception and data for details.", ex);
                e.Data.Add("X", x);
                e.Data.Add("Y", y);
                throw e;
            }
        }

        /// <summary>
        /// Create new instance
        /// </summary>
        /// <param name="expr">Comparision expression</param>
        /// <returns>New GenericComperer instance</returns>
        public static GenericComperer<T> Get(Func<T, IComparable> expr)
        {
            return new GenericComperer<T>(expr);
        }
    }
}
