using System;
using System.Collections.Generic;

namespace CameraImporter.Shared.Interface
{
    public interface ILogger
    {
       
        void Log(string message, LogLevel level);
        void ClearLog();
        List<LogData> LogHistory { get; }
        string ToString();
        event EventHandler<LogUpdatedEventArgs> LogUpdated;
    }
}
