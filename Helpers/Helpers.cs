using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Helpers
{
    /// <summary>
    /// Some extensions
    /// </summary>
    public static partial class Extensions
    {
        /// <summary>
        /// Get full exception text
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="whereCathed">Cather information (e.g. function name)</param>
        /// <param name="includeStackTrace">Include stack trace</param>
        /// <param name="clearText">Include only exception (and inner exceptions) text</param>
        /// <param name="includeData">Include exception data block is exists</param>
        /// <returns>Exception text</returns>
        public static string GetExceptionText(this Exception ex, string whereCathed = null, bool includeStackTrace = true, bool clearText = false, bool includeData = true)
        {
            if (ex == null)
                return string.Empty;

            string result = string.Empty;
            Exception innerEx = ex;
            while (innerEx != null)
            {
                if (clearText)
                    result += (string.IsNullOrWhiteSpace(result) ? string.Empty : Environment.NewLine) + innerEx.Message;
                else
                    result += (
                        (string.IsNullOrWhiteSpace(result))
                        ? string.Format("exception '{0}' occured;", innerEx.Message)
                        : Environment.NewLine + string.Format("inner exception '{0}' occured;", innerEx.Message)
                        ) + (string.IsNullOrWhiteSpace(innerEx.Source) ? string.Empty : string.Format(" Source: '{0}';", innerEx.Source));

                if (!clearText && includeData && innerEx.Data != null && innerEx.Data.Count > 0)
                {
                    result += Environment.NewLine + string.Format("exception data (items count: {0}):", innerEx.Data.Count);
                    int n = 0;
                    foreach (var key in innerEx.Data.Keys)
                    {
                        var value = innerEx.Data[key];
                        result += string.Format("{0}data item ({1}) key: '{2}'{0}{3}", Environment.NewLine, n, key, value ?? "<NULL>");
                        n++;
                    }
                }
                innerEx = innerEx.InnerException;
            }
            if (!string.IsNullOrWhiteSpace(ex.StackTrace) && includeStackTrace)
                result += string.Format("{0}{1}{0}", Environment.NewLine, ex.StackTrace);

            if (whereCathed != null)
                result = string.Format(Log.WhereCatchedFormat, whereCathed, result);

            return result;
        }


        /// <summary>
        /// Check similarity for string and mask (RegEx used)
        /// </summary>
        /// <param name="source">Source string</param>
        /// <param name="mask">Mask<br/><i>Should be like <b>test*mask*</b> or other</i></param>
        /// <param name="ignoreCase">Ignore case for string and mask while checking</param>
        /// <returns>Return true if source string like mask</returns>
        public static bool Like(this string source, string mask, bool ignoreCase = true)
        {
            return StringLikes(source, mask, ignoreCase);
        }

        /// <summary>
        /// Check similarity for string and mask (RegEx used)
        /// </summary>
        /// <param name="source">Source string</param>
        /// <param name="mask">Mask<br/><i>Should be like <b>test*mask*</b> or other</i></param>
        /// <param name="ignoreCase">Ignore case for string and mask while checking</param>
        /// <returns>Return true if source string like mask</returns>
        public static bool StringLikes(string source, string mask, bool ignoreCase = true)
        {
            try
            {
                string str = "^" + Regex.Escape(mask);
                str = str.Replace("\\*", ".*").Replace("\\?", ".") + "$";

                bool result = (Regex.IsMatch(source, str, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None));
                return result;
            }
            catch (Exception ex)
            {
                ex.Data.Add("source", source);
                ex.Data.Add("mask", mask);
                ex.Data.Add("ignoreCase", ignoreCase);
                Log.Add(ex, "Helpers.Extensions.StringLikes");
                return false;
            }
        }

        /// <summary>
        /// Copy object properties from selected item to destination object. <br/>Object can be <b>not similar</b> types.
        /// </summary>
        /// <typeparam name="fromType">Type of source object</typeparam>
        /// <typeparam name="toType">Type of destination object</typeparam>
        /// <param name="from">Source object</param>
        /// <param name="to">Destincation object</param>
        public static Notification CopyObject<fromType, toType>(this fromType from, toType to)
        {
            return CopyObject<fromType, toType>(from, to, new string[] { });
        }

        /// <summary>
        /// Copy object properties from selected item to destination object. <br/>Object can be <b>not similar</b> types.
        /// </summary>
        /// <typeparam name="fromType">Type of source object</typeparam>
        /// <typeparam name="toType">Type of destination object</typeparam>
        /// <param name="from">Source object</param>
        /// <param name="to">Destincation object</param>
        public static Notification CopyObjectTo<fromType, toType>(this fromType from, toType to)
        {
            return CopyObject<fromType, toType>(from, to, new string[] { });
        }

        /// <summary>
        /// Copy object properties from selected item to destination object. <br/>Object can be <b>not similar</b> types.
        /// </summary>
        /// <typeparam name="fromType">Type of source object</typeparam>
        /// <typeparam name="toType">Type of destination object</typeparam>
        /// <param name="from">Source object</param>
        /// <param name="to">Destincation object</param>
        public static Notification CopyObjectFrom<fromType, toType>(this toType to, fromType from)
        {
            return CopyObject<fromType, toType>(from, to, new string[] { });
        }

        /// <summary>
        /// Copy object properties from selected item to destination object. <br/>Object can be <b>not similar</b> types.
        /// </summary>
        /// <typeparam name="fromType">Type of source object</typeparam>
        /// <typeparam name="toType">Type of destination object</typeparam>
        /// <param name="from">Source object</param>
        /// <param name="to">Destincation object</param>
        /// <param name="excludePropertyes">Exclude some property names. Items can use LIKE syntax (ex: '*name*' or 'param*')</param>
        public static Notification CopyObjectTo<fromType, toType>(this fromType from, toType to, string[] excludePropertyes)
        {
            return CopyObject<fromType, toType>(from, to, excludePropertyes);
        }

        /// <summary>
        /// Copy object properties from selected item to destination object. <br/>Object can be <b>not similar</b> types.
        /// </summary>
        /// <typeparam name="fromType">Type of source object</typeparam>
        /// <typeparam name="toType">Type of destination object</typeparam>
        /// <param name="from">Source object</param>
        /// <param name="to">Destincation object</param>
        /// <param name="excludePropertyes">Exclude some property names. Items can use LIKE syntax (ex: '*name*' or 'param*')</param>
        /// <returns>Notifications</returns>
        public static Notification CopyObjectFrom<fromType, toType>(this toType to, fromType from, string[] excludePropertyes)
        {
            return CopyObject<fromType, toType>(from, to, excludePropertyes);
        }

        /// <summary>
        /// Copy object properties from selected item to destination object. <br/>Object can be <b>not similar</b> types.
        /// </summary>
        /// <typeparam name="fromType">Type of source object</typeparam>
        /// <typeparam name="toType">Type of destination object</typeparam>
        /// <param name="from">Source object</param>
        /// <param name="to">Destincation object</param>
        /// <param name="excludePropertyes">Exclude some property names. Items can use LIKE syntax (ex: '*name*' or 'param*')</param>
        /// <returns>Notifications</returns>
        public static Notification CopyObject<fromType, toType>(this fromType from, toType to, string[] excludePropertyes)
        {
            var res = new Notification();

            if (from == null || to == null)
                return res;

            var piToItems = to.GetType()
                .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                .Where(pi => pi.CanWrite && !excludePropertyes.Any(ep => pi.Name.Like(ep) ))
                .ToArray();

            var piFromItems = from.GetType()
                .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                .ToArray();

            var items = piToItems
                .Join(piFromItems, t => t.Name, f => f.Name, (t, f) => new { From = f, To = t });

            foreach (var pi in items)
                try
                {
                    object value = pi.From.GetValue(from, null);

                    if (value == null)
                        pi.To.SetValue(to, value, null);
                    else
                    {
                        try
                        {
                            pi.To.SetValue(to, value, null);
                        }
                        catch
                        {
                            pi.To.SetValue(to, System.Convert.ChangeType(value, pi.To.PropertyType), null);
                        }
                    }
                }
                catch(Exception ex)
                {
                    var e = new Exception(string.Format("Error converting property '{0}' ('{1}') to '{2}' ('{3}')", pi.From.Name, pi.From.PropertyType, pi.To.Name, pi.To.PropertyType), ex);
                    res.AddNotification(e.GetExceptionText());
                }
            return res;
        }

        /// <summary>
        /// Get attribute for enumeration value.
        /// </summary>
        /// <typeparam name="T">Attribute type to select</typeparam>
        /// <typeparam name="TExpected">Expected value type (e.g. string)</typeparam>
        /// <param name="enumeration">Enumeration type</param>
        /// <param name="expression">Selective expression</param>
        /// <returns>Attribute value for scpecified enum value</returns>
        public static TExpected GetAttributeValue<T, TExpected>(this Enum enumeration, Func<T, TExpected> expression)
        where T : Attribute
        {
            T attribute =
              enumeration
                .GetType()
                .GetMember(enumeration.ToString())
                .Where(member => member.MemberType == MemberTypes.Field)
                .FirstOrDefault()
                .GetCustomAttributes(typeof(T), false)
                .Cast<T>()
                .SingleOrDefault();

            if (attribute == null)
                return default(TExpected);

            return expression(attribute);
        }
    }
}
