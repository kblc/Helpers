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
                logFileName = newValue;
            }
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
            Add(session, string.Format("elapsed time: {0} ms.", (DateTime.Now - Sessions[session].SessionStart).TotalMilliseconds));
            lock (sessionsLock)
            {
                if (Sessions[session].IsBlockInfo)
                {
                    if (writeThisBlock)
                    {
                        var logMessages = 
                            new string[] { 
                                "##########################################################",
                                string.Format("### {0}", Sessions[session].SessionName)
                            }
                            .Union(Sessions[session].Log.ToArray())
                            .Union(new string[] { 
                                "##########################################################" 
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

    /// <summary>
    /// Class to calculate percentage progress for many values
    /// </summary>
    public class PercentageProgress
    {
        /// <summary>
        /// Event arguments for percentage progress change events
        /// </summary>
        public class PercentageProgressEventArgs : EventArgs
        {
            public PercentageProgressEventArgs() { }
            public PercentageProgressEventArgs(float value)
            {
                Value = value;
            }

            /// <summary>
            /// Current percentage progress value
            /// </summary>
            public readonly float Value = 0;
        }

        public PercentageProgress() { }

        private object childLocks = new Object();
        private List<PercentageProgress> childs = new List<PercentageProgress>();

        private float value = 0;
        
        /// <summary>
        /// Get or set current percentage value for this part (from 0 to 100)
        /// </summary>
        public float Value
        {
            get
            {
                lock (childLocks)
                    return (childs.Count == 0) ? value : (childs.Sum( i => i.Value ) / childs.Count);
            }
            set
            {
                bool needRaise = false;

                if (value > 100 || value < 0)
                    throw new ArgumentException("Значение должно быть в диапазоне от 0 до 100");

                lock (childLocks)
                    if (childs.Count == 0)
                    {
                        needRaise = this.value != value;
                        this.value = value;
                    }
                    else
                    {
                        lockRaise = true;
                        try
                        { 
                            foreach (var c in childs)
                            { 
                                needRaise = needRaise || (c.value != value);
                                c.value = value;
                            }
                        }
                        finally
                        {
                            lockRaise = false;
                        }
                    }

                if (needRaise)
                    RaiseChange();
            }
        }
        
        /// <summary>
        /// Get if current part has child
        /// </summary>
        public bool HasChilds
        {
            get { lock(childLocks) return childs.Count > 0; }
        }

        /// <summary>
        /// Get new child with default value
        /// </summary>
        /// <param name="value">Percentage value for new child</param>
        /// <returns>Child with default value for this item</returns>
        public PercentageProgress GetChild(float value = 0)
        {
            PercentageProgress result = new PercentageProgress() { Value = value };
            result.Change += child_Change;
            lock (childLocks)
            { 
                childs.Add(result);
            }
            RaiseChange();
            return result;
        }

        /// <summary>
        /// Remove child
        /// </summary>
        /// <param name="child">Child to remove</param>
        public void RemoveChild(PercentageProgress child)
        {
            lock (childLocks)
            if (childs.Contains(child))
            {
                child.Change -= child_Change;
                childs.Remove(child);
            }
            RaiseChange();
        }

        private void child_Change(object sender, PercentageProgressEventArgs e)
        {
            RaiseChange();
        }

        private bool lockRaise = false;
        private void RaiseChange()
        {
            if (Change != null && !lockRaise)
                Change(this, new PercentageProgressEventArgs(Value));
        }

        /// <summary>
        /// Occurs when a property Value changes
        /// </summary>
        public event EventHandler<PercentageProgressEventArgs> Change;
    }

    public class PropertyChangedBase : System.ComponentModel.INotifyPropertyChanged
    {
        private class RaiseItem
        {
            public string PropertyName;
            public string[] WhenPropertyNames;
        }

        #region Property changed

        private List<RaiseItem> afterItems = new List<RaiseItem>();
        private List<RaiseItem> beforeItems = new List<RaiseItem>();

        /// <summary>
        /// Occurs when a property value changes
        /// </summary>
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChange(string propertyName)
        {
            if (PropertyChanged != null)
            {
                foreach (var item in beforeItems.Where(bI => bI.WhenPropertyNames.Any(wP => propertyName.Like(wP))))
                    RaisePropertyChange(item.PropertyName);

                //foreach (var item in beforeItems)
                //    if (item.WhenPropertyNames.Contains(propertyName))
                //        RaisePropertyChange(item.PropertyName);
                    
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));

                foreach (var item in afterItems.Where(bI => bI.WhenPropertyNames.Any(wP => propertyName.Like(wP))))
                    RaisePropertyChange(item.PropertyName);

                //foreach (var item in afterItems)
                //    if (item.WhenPropertyNames.Contains(propertyName))
                //        RaisePropertyChange(item.PropertyName);
            }
        }
        /// <summary>
        /// Manage to raise property <b>propertyName</b> before any of <b>afterPropertyNames</b> raised
        /// </summary>
        /// <param name="afterPropertyNames">Array of property to wath.<br/><i>Can be mask like "*Value"</i></param>
        /// <param name="propertyName">Property name to raise</param>
        protected void RaisePropertyAfterChange(string[] afterPropertyNames, string propertyName)
        {
            afterItems.Add(new RaiseItem() { PropertyName = propertyName, WhenPropertyNames = afterPropertyNames });
        }

        /// <summary>
        /// Manage to raise property <b>propertyName</b> after any of <b>afterPropertyNames</b> raised
        /// </summary>
        /// <param name="afterPropertyNames">Array of property to wath.<br/><i>Can be mask like "*Value"</i></param>
        /// <param name="propertyName">Property name to raise</param>
        protected void RaisePropertyBeforeChange(string[] beforePropertyNames, string propertyName)
        {
            beforeItems.Add(new RaiseItem() { PropertyName = propertyName, WhenPropertyNames = beforePropertyNames });
        }

        #endregion
    }

    public static partial class Extensions
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            //HashSet<TKey> knownKeys = new HashSet<TKey>();
            //foreach (TSource element in source)
            //{
            //    if (knownKeys.Add(keySelector(element)))
            //    {
            //        yield return element;
            //    }
            //}

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
        /// Check object is Design mode now
        /// </summary>
        /// <param name="obj">Source object</param>
        /// <returns>Returns true if object is in design mode now</returns>
        public static bool IsDesignMode(this object obj)
        {
            return System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject());
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
