using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Helpers
{
    public static class Log
    {
        internal class SessionInfo : IDisposable
        {
            public DateTime SessionStart = DateTime.Now;
            public string SessionName = string.Empty;
            public SessionInfo() 
            {
            }
            public SessionInfo(string sessionName)
                : this()
            {
                SessionName = sessionName;
            }
            public SessionInfo(string sessionName, DateTime sessionStart)
                : this(sessionName)
            {
                SessionStart = sessionStart;
            }

            public bool IsBlockInfo { get; set; }

            private List<string> log = null;
            public List<string> Log
            {
                get
                {
                    return log ?? (log = new List<string>());
                }
            }

            public void Dispose()
            {
                if (log != null)
                    log.Clear();
            }
        }

        private static object sessionsLock = new Object();
        private static Dictionary<Guid, SessionInfo> Sessions = new Dictionary<Guid, SessionInfo>();

        private const string WhereCatchedFormat = "{0} :: {1}";

        private static string logFileName = string.Empty;
        
        /// <summary>
        /// Get or Set log file name. <i>Log file located in current assembly directory</i>
        /// </summary>
        public static string LogFileName
        {
            get
            {
                return logFileName;
            }
            set
            {
                string newValue = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
                if (logFileName == newValue)
                    return;

                if (Touch(newValue))
                    logFileName = newValue;
            }
        }

        private static bool Touch(string newValue)
        {
            if (string.IsNullOrWhiteSpace(newValue))
                return true;

            bool result = false;
            string newLogPath = Path.Combine(CurrentPath, newValue);
            lock (fileLogLock)
                try
                {
                    using (StreamWriter w = File.AppendText(newLogPath))
                    {
                        bool isBlock;
                        w.WriteLine(GetFullLogMessage(Guid.Empty, string.Format("Log started at file '{0}'", newLogPath), out isBlock));
                        result = true;
                    }
                }
                catch(Exception ex)
                {
                    WriteToLogOutput(new string[] { string.Format("Can''t touch file '{0}':{1}{2}", newLogPath, Environment.NewLine, ex.GetExceptionText()) });
                }
            return result;
        }

        private static object fileLogLock = new Object();
        private static object consoleLogLock = new Object();

        private static string currentPath = null;
        /// <summary>
        /// Get current assembly path
        /// </summary>
        public static string CurrentPath
        {
            get
            {
                return currentPath ?? (currentPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            }
        }

        /// <summary>
        /// Get full log file path
        /// </summary>
        public static string LogFilePath
        {
            get
            {
                return !string.IsNullOrWhiteSpace(LogFileName) ? Path.Combine(CurrentPath, LogFileName) : string.Empty;
            }
        }

        public static Guid SessionStart(string sessionName, bool isBlockInfo = false)
        {
            Guid result = Guid.NewGuid();
            lock (sessionsLock)
                Sessions.Add(result, new SessionInfo(sessionName) { IsBlockInfo = isBlockInfo });
            return result;
        }

        public static void SessionEnd(Guid session, bool writeThisBlock = true)
        {
            if (!Sessions.ContainsKey(session))
            {
                WriteToLogOutput(new string[] { (new Exception(string.Format("Session '{0}' not exists in session dictionary", session.ToString()))).GetExceptionText() });
                return;
            }

            Add(session, string.Format("elapsed time: {0} ms.", (DateTime.Now - Sessions[session].SessionStart).TotalMilliseconds));
            lock (sessionsLock)
            {
                if (Sessions[session].IsBlockInfo)
                {
                    if (writeThisBlock)
                    {
                        var logMessages = 
                            new string[] { 
                                "B#########################################################",
                                string.Format("### {0}", Sessions[session].SessionName)
                            }
                            .Union(Sessions[session].Log.ToArray())
                            .Union(new string[] { 
                                "E#########################################################" 
                            })
                            .ToArray();

                        WriteToLogOutput(logMessages);
                    }
                    Sessions[session].Log.Clear();
                }
                Sessions.Remove(session);
            }
        }

        public static void Add(string logMessage)
        {
            Add(Guid.Empty, logMessage);
        }
        
        public static void AddWithCatcher(string whereCathched, string logMessage)
        {
            Add(Guid.Empty, string.Format(WhereCatchedFormat, whereCathched, logMessage));
        }

        private static string GetFullLogMessage(Guid session, string logMessage, out bool isBlock)
        {
            string message = string.Format("[{0}] ", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));

            if (session != Guid.Empty)
                lock (sessionsLock)
                {
                    isBlock = Sessions[session].IsBlockInfo;
                    message +=
                        isBlock
                        ? logMessage
                        : string.Format(WhereCatchedFormat, Sessions[session].SessionName, logMessage);
                }
            else
            {
                isBlock = false;
                message += logMessage;
            }

            return message;
        }

        private static void WriteToLogOutput(string[] strings)
        {
            lock (consoleLogLock)
            {
                foreach (var str in strings)
                    Console.WriteLine(str);
            }
            //start /B /wait ????.exe > out.txt & type out.txt
            if (!string.IsNullOrWhiteSpace(LogFileName))
                lock (fileLogLock)
                    using (StreamWriter w = File.AppendText(LogFilePath))
                    {
                        foreach (var str in strings)
                            w.WriteLine(str);
                    }
        }

        public static void Add(string logMessage, string whereCathed)
        {
            Add(string.Format(WhereCatchedFormat, whereCathed, logMessage));
        }

        public static void Add(Guid session, string logMessage)
        {
            bool isBlock;
            logMessage = GetFullLogMessage(session, logMessage, out isBlock);

            if (isBlock && session != null)
                lock(sessionsLock)
                {
                    Sessions[session].Log.Add(logMessage);
                }
            else
            {
                WriteToLogOutput(new string[] { logMessage });
            }
        }

        public static void Add(Guid session, Exception ex)
        {
            Add(session, ex.GetExceptionText());
        }

        public static void Add(Exception ex)
        {
            Add(ex.GetExceptionText());
        }

        public static void Add(Exception ex, string whereCathed)
        {
            Add(string.Format(WhereCatchedFormat, whereCathed, ex.GetExceptionText()));
        }

        /// <summary>
        /// Clear (remove) log file
        /// </summary>
        public static void Clear()
        {
            if (!string.IsNullOrEmpty(LogFileName) && File.Exists(LogFilePath))
                lock (fileLogLock)
                    File.Delete(LogFilePath);
        }

        public static string GetExceptionText(this Exception ex, string whereCathed = null)
        {
            if (ex == null)
                return string.Empty;

            string result = string.Empty;
            Exception innerEx = ex;
            while (innerEx != null)
            {
                result += 
                    (string.IsNullOrWhiteSpace(result))
                    ?                       string.Format("exception '{0}' occured; Source: '{1}';", innerEx.Message, innerEx.Source)
                    : Environment.NewLine + string.Format("inner exception '{0}' occured; Source: '{1}';", innerEx.Message, innerEx.Source);

                innerEx = innerEx.InnerException;
            }
            result += string.Format("{0}{1}{0}", Environment.NewLine, ex.StackTrace);

            if (whereCathed != null)
                result = string.Format(WhereCatchedFormat, whereCathed, result);

            return result;
        }
    }

    public static partial class Extensions
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return source.GroupBy(keySelector).Select(grp => grp.First());
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
                Log.Add(ex.GetExceptionText("Helpers.Extensions.StringLikes()"));
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
        public static void CopyObject<fromType, toType>(this fromType from, toType to)
        {
            CopyObject<fromType, toType>(from, to, new string[] { });
        }

        /// <summary>
        /// Copy object properties from selected item to destination object. <br/>Object can be <b>not similar</b> types.
        /// </summary>
        /// <typeparam name="fromType">Type of source object</typeparam>
        /// <typeparam name="toType">Type of destination object</typeparam>
        /// <param name="from">Source object</param>
        /// <param name="to">Destincation object</param>
        /// <param name="excludePropertyes">Exclude some property names. Items can use LIKE syntax (ex: '*name*' or 'param*')</param>
        public static void CopyObject<fromType, toType>(this fromType from, toType to, string[] excludePropertyes)
        {
            if (from == null || to == null)
                return;

            var piToItems = 
                typeof(toType)
                .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                .Where(pi => pi.CanWrite && !excludePropertyes.Any(ep => pi.Name.Like(ep) ))
                .ToArray();
            var piFromItems = 
                typeof(fromType)
                .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                .ToArray();

            foreach (var piTo in piToItems)
            {
                var piFrom = piFromItems.FirstOrDefault(p => p.Name == piTo.Name);
                if (piFrom != null)
                {
                    object value = piFrom.GetValue(from, null);
                    if (value == null)
                        piTo.SetValue(to, value, null);
                    else
                        piTo.SetValue(to, System.Convert.ChangeType(value, piTo.PropertyType), null);
                }
            }
        }
    }
}
