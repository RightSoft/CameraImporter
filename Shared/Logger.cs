using System;
using System.Collections.Generic;
using CameraImporter.Shared.Interface;

namespace CameraImporter.Shared
{
    public enum LogLevel
    {
        Info,
        Warning, 
        Error
    }

    public class Logger : ILogger
    {
        public event EventHandler<LogUpdatedEventArgs> LogUpdated;
        public List<LogData> LogHistory { get; }

        public Logger()
        {
            LogHistory = new List<LogData>();
        }

        public void Log(string message, LogLevel level)
        {
            var logData = new LogData(){Message = message, Level = level};
            LogHistory.Add(logData);
            LogUpdated?.Invoke(this, new LogUpdatedEventArgs(logData) );
        }

        public override string ToString()
        {
           string returnValue = String.Empty;

            for (var i = 0; i < LogHistory.Count; i++)
            {
                var logData = LogHistory[i];
                var isLastItem = (i==LogHistory.Count-1);
                returnValue += $"[{logData.Level}]{logData.Message}{(isLastItem ? "" : Environment.NewLine)}";
            }

            return returnValue;
        }

        public void ClearLog()
        {
            LogHistory.Clear();
        }
    }

    public struct LogData
    {
        public LogLevel Level;
        public string Message;
    }

    public class LogUpdatedEventArgs : EventArgs
    {
        public LogData logData;

        public LogUpdatedEventArgs(LogData data)
        {
            logData = data;
        }
    }
}
