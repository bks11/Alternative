using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;


namespace DoRelation.Common
{
    public class Logger : IDisposable
    {
        private string _logFilePath;
        private LogFormatter _logFormater;
        private FileStream fileStream = null;
        protected List<LogLevel> _logLevelHandlingList;
        protected string _formattedLogString;

        public Logger()
        {
            _logFilePath = ConfigurationManager.AppSettings[Const.LOG_PATH_NAME];
            _logFormater = new LogFormatter();
            this._logLevelHandlingList = GetLogLevelHandlingList();
        }

        private List<LogLevel> GetLogLevelHandlingList()
        {
            List<LogLevel> logLevelHandlingList = new List<LogLevel>();
            logLevelHandlingList.Add(LogLevel.Debug);
            logLevelHandlingList.Add(LogLevel.Error);
            logLevelHandlingList.Add(LogLevel.Fatal);
            logLevelHandlingList.Add(LogLevel.Info);
            logLevelHandlingList.Add(LogLevel.MaxLevel);
            logLevelHandlingList.Add(LogLevel.MinLevel);
            logLevelHandlingList.Add(LogLevel.Trace);
            logLevelHandlingList.Add(LogLevel.Warn);
            return logLevelHandlingList;
        }

        protected void BuildFormattedLog(params object[] objects)
        {
            _formattedLogString = _logFormater.ToFormattedString(objects);
            if (string.IsNullOrEmpty(_formattedLogString)) return;
            try
            {
                File.AppendAllText(_logFilePath, _formattedLogString);
            }
            catch (Exception ex)
            {
                //TODO
            }
        }

        #region Log Methods

        public void Log(LogLevel logLevel, string message)
        {
            if (!_logLevelHandlingList.Contains(logLevel) || _logLevelHandlingList.Contains(LogLevel.Off)) return;
            BuildFormattedLog(logLevel, message);
        }

        public void Log(LogLevel logLevel, string message, object obj)
        {
            if (!_logLevelHandlingList.Contains(logLevel) || _logLevelHandlingList.Contains(LogLevel.Off)) return;
            BuildFormattedLog(logLevel, string.Format(message, obj));
        }

        public void Log(LogLevel logLevel, string message, params object[] args)
        {
            if (!_logLevelHandlingList.Contains(logLevel) || _logLevelHandlingList.Contains(LogLevel.Off)) return;
            BuildFormattedLog(logLevel, string.Format(message, args));
        }

        public void Log(LogLevel logLevel, string format, IFormatProvider formatProvider, params object[] args)
        {
            if (!_logLevelHandlingList.Contains(logLevel) || _logLevelHandlingList.Contains(LogLevel.Off)) return;
            BuildFormattedLog(logLevel, string.Format(formatProvider, format, args));
        }

        public void Log(LogLevel logLevel, Exception exception)
        {
            if (!_logLevelHandlingList.Contains(logLevel) || _logLevelHandlingList.Contains(LogLevel.Off)) return;
            BuildFormattedLog(logLevel, null, null, exception);
        }

        public void Log(LogLevel logLevel, Exception exception, string message)
        {
            if (!_logLevelHandlingList.Contains(logLevel) || _logLevelHandlingList.Contains(LogLevel.Off)) return;
            BuildFormattedLog(logLevel, message, null, exception);
        }

        public void Log(LogLevel logLevel, Exception exception, string message, object obj)
        {
            if (!_logLevelHandlingList.Contains(logLevel) || _logLevelHandlingList.Contains(LogLevel.Off)) return;
            BuildFormattedLog(logLevel, string.Format(message, obj), null, exception);
        }

        public void Log(LogLevel logLevel, Exception exception, string message, params object[] args)
        {
            if (!_logLevelHandlingList.Contains(logLevel) || _logLevelHandlingList.Contains(LogLevel.Off)) return;
            BuildFormattedLog(logLevel, string.Format(message, args), null, exception);
        }

        public void Log(LogLevel logLevel, Exception exception, string format, IFormatProvider formatProvider, params object[] args)
        {
            if (!_logLevelHandlingList.Contains(logLevel) || _logLevelHandlingList.Contains(LogLevel.Off)) return;
            BuildFormattedLog(logLevel, string.Format(formatProvider, format, args), null, exception);
        }

        public void Log(LogLevel logLevel, Exception exception, MethodBase methodBase)
        {
            if (!_logLevelHandlingList.Contains(logLevel) || _logLevelHandlingList.Contains(LogLevel.Off)) return;
            BuildFormattedLog(logLevel, null, methodBase, exception);
        }

        public void Log(LogLevel logLevel, Exception exception, MethodBase methodBase, string message)
        {
            if (!_logLevelHandlingList.Contains(logLevel) || _logLevelHandlingList.Contains(LogLevel.Off)) return;
            BuildFormattedLog(logLevel, message, methodBase, exception);
        }

        public void Log(LogLevel logLevel, Exception exception, MethodBase methodBase, string message, object obj)
        {
            if (!_logLevelHandlingList.Contains(logLevel) || _logLevelHandlingList.Contains(LogLevel.Off)) return;
            BuildFormattedLog(logLevel, string.Format(message, obj), methodBase, exception);
        }

        public void Log(LogLevel logLevel, Exception exception, MethodBase methodBase, string message, params object[] args)
        {
            if (!_logLevelHandlingList.Contains(logLevel) || _logLevelHandlingList.Contains(LogLevel.Off)) return;
            BuildFormattedLog(logLevel, string.Format(message, args), methodBase, exception);
        }

        public void Log(LogLevel logLevel, Exception exception, MethodBase methodBase, string format, IFormatProvider formatProvider, params object[] args)
        {
            if (!_logLevelHandlingList.Contains(logLevel) || _logLevelHandlingList.Contains(LogLevel.Off)) return;
            BuildFormattedLog(logLevel, string.Format(formatProvider, format, args), methodBase, exception);
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (fileStream != null)
            {
                fileStream.Close();
                fileStream.Dispose();
            }
        }

        #endregion
    }
}
