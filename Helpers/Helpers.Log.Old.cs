using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Helpers.Old
{
    /// <summary>
    /// Log static class for loggin anything
    /// </summary>
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

            public event EventHandler OnDispose;

            public void Dispose()
            {
                var e = OnDispose;
                if (e != null)
                    e(this, new EventArgs());
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
        /// Start new log session
        /// </summary>
        /// <param name="sessionName">Session name (e.g. function name)</param>
        /// <param name="isBlockInfo">Write log after close session or write directly</param>
        /// <returns>Session identifier</returns>
        public static Guid SessionStart(string sessionName, bool isBlockInfo = false)
        {
            lock (sessionsLock)
            {
                Guid result = Guid.NewGuid();
                Sessions.Add(result, new SessionInfo(sessionName) { IsBlockInfo = isBlockInfo });
                return result;
            }
        }

        /// <summary>
        /// Close log session
        /// </summary>
        /// <param name="session">Session identifier</param>
        /// <param name="writeThisBlock">Use false to hide this session log</param>
        public static void SessionEnd(Guid session, bool writeThisBlock = true)
        {
            var contains = false;
            lock (sessionsLock)
                contains = Sessions.ContainsKey(session);

            if (!contains)
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

        /// <summary>
        /// Add log message
        /// </summary>
        /// <param name="logMessage">Message to log</param>
        public static void Add(string logMessage)
        {
            Add(Guid.Empty, logMessage);
        }

        /// <summary>
        /// Add log message with cather information
        /// </summary>
        /// <param name="whereCathed">Cather information (e.g. function name)</param>
        /// <param name="logMessage">Message to log</param>
        public static void AddWithCatcher(string whereCathed, string logMessage)
        {
            Add(Guid.Empty, string.Format(WhereCatchedFormat, whereCathed, logMessage));
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

        /// <summary>
        /// Add log message with cather information
        /// </summary>
        /// <param name="whereCathed">Cather information (e.g. function name)</param>
        /// <param name="logMessage">Message to log</param>
        public static void Add(string logMessage, string whereCathed)
        {
            AddWithCatcher(whereCathed: whereCathed, logMessage: logMessage);
        }

        /// <summary>
        /// Add log message to session
        /// </summary>
        /// <param name="session">Session identifier</param>
        /// <param name="logMessage">Message to log</param>
        public static void Add(Guid session, string logMessage)
        {
            bool isBlock;
            logMessage = GetFullLogMessage(session, logMessage, out isBlock);

            if (isBlock && session != null)
                lock (sessionsLock)
                {
                    Sessions[session].Log.Add(logMessage);
                }
            else
            {
                WriteToLogOutput(new string[] { logMessage });
            }
        }

        /// <summary>
        /// Add log message from exception to session
        /// </summary>
        /// <param name="session">Session identifier</param>
        /// <param name="ex">Exception to log</param>
        public static void Add(Guid session, Exception ex)
        {
            Add(session, ex.GetExceptionText());
        }

        /// <summary>
        /// Add log message from exception
        /// </summary>
        /// <param name="ex">Exception to log</param>
        public static void Add(Exception ex)
        {
            Add(ex.GetExceptionText());
        }

        /// <summary>
        /// Add log message from exception with catcher information
        /// </summary>
        /// <param name="ex">Exception to log</param>
        /// <param name="whereCathed">Cather information (e.g. function name)</param>
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

    }

}
