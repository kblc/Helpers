using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Helpers
{
    /// <summary>
    /// Log static class for loggin anything
    /// </summary>
    public static class Log
    {
        public class SessionInfo : IDisposable
        {
            public readonly DateTime SessionStart = DateTime.Now;
            private DateTime PartStart = DateTime.Now;
            public readonly string SessionName = string.Empty;
            private readonly List<string> log = new List<string>();
            private readonly Action<IEnumerable<string>> Output = null;
            public bool Enabled { get; set; }

            public SessionInfo(string sessionName = "", bool isEnabled = true, Action<IEnumerable<string>> output = null)
            {
                SessionName = sessionName;
                Output = output ?? new Action<IEnumerable<string>>((s) => { });
                Enabled = isEnabled;
            }

            /// <summary>
            /// Add log message
            /// </summary>
            /// <param name="message"></param>
            public void Add(string message, string whereCathed = null)
            {
                log.Add(whereCathed == null ? message : string.Format(WhereCatchedFormat, whereCathed, message));
            }

            /// <summary>
            /// Add log message from exception with catcher information
            /// </summary>
            /// <param name="ex">Exception to log</param>
            /// <param name="whereCathed">Cather information (e.g. function name)</param>
            public void Add(Exception ex, string whereCathed = null)
            {
                Add(ex.GetExceptionText(), whereCathed);
            }

            /// <summary>
            /// Clear log
            /// </summary>
            public void Clear()
            {
                if (log != null)
                    log.Clear();
            }

            public TimeSpan TotalElapsed { get { return (DateTime.Now - SessionStart); } }
            public TimeSpan PartElapsed { get { return (DateTime.Now - PartStart); } }

            public void LogElapsed()
            {
                Add(string.Format("### part elapsed: {0} ms", PartElapsed.TotalMilliseconds));
                PartStart = DateTime.Now;
            }

            private void LogTotalElapsed()
            {
                Add(string.Format("### total elapsed: {0} ms", TotalElapsed.TotalMilliseconds));
            }

            public event EventHandler OnDispose;
            public void Dispose()
            {
                var e = OnDispose;
                if (e != null)
                    e(this, new EventArgs());

                if (Enabled && log.Count > 0)
                {
                    log.Insert(0, "B#########################################################");
                    if (!string.IsNullOrWhiteSpace(SessionName))
                        log.Insert(1, string.Format("### {0}", SessionName));
                    LogTotalElapsed();
                    Add("E#########################################################");
                    Output(log);
                    Clear();
                }
            }
        }

        internal const string WhereCatchedFormat = "{0} :: {1}";
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
                        w.WriteLine(GetFormatedLogMessage(string.Format("Log started at file '{0}'", newLogPath)));
                        result = true;
                    }
                }
                catch (Exception ex)
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

        /// <summary>
        /// Add log message
        /// </summary>
        /// <param name="message"></param>
        public static void Add(string message, string whereCathed = null)
        {
            var formatedMessage = GetFormatedLogMessage(whereCathed == null ? message : string.Format(WhereCatchedFormat, whereCathed, message));
            WriteToLogOutput(new string[] { formatedMessage });
        }

        /// <summary>
        /// Add log message from exception with catcher information
        /// </summary>
        /// <param name="ex">Exception to log</param>
        /// <param name="whereCathed">Cather information (e.g. function name)</param>
        public static void Add(Exception ex, string whereCathed = null)
        {
            Add(ex.GetExceptionText(), whereCathed);
        }

        private static string GetFormatedLogMessage(string logMessage)
        {
            return string.Format("[{0}] {1}", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), logMessage);
        }

        private static void WriteToLogOutput(IEnumerable<string> strings)
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

        public static SessionInfo Session(string sessionName = "", bool isEnabled = true, Action<IEnumerable<string>> output = null)
        {
            return new SessionInfo(sessionName, isEnabled, output ?? WriteToLogOutput);
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
    }
}

