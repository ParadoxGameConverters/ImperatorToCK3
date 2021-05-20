using System;
using System.Collections.Generic;
using System.IO;

namespace commonItems
{
    public enum LogLevel
    {
        Error,
        Warning,
        Info,
        Debug,
        Progress
    }
    
    public class Log
    {
        static bool logFileCreated = false;
        public static void WriteLine(LogLevel level, string message) {

            if (!logFileCreated)
            {
                System.IO.File.WriteAllText("log.txt", string.Empty);
                logFileCreated = true;
            }

            using StreamWriter logFile = File.AppendText("log.txt");
            logFile.Write(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            logFile.Write(logLevelStrings[level]);
            logFile.Write(message);
            logFile.Write("\n");
        }
        
        public static Dictionary<LogLevel, string> logLevelStrings = new(){
            { LogLevel.Error, "    [ERROR] " },
            { LogLevel.Warning, "  [WARNING] " },
            { LogLevel.Info, "     [INFO] " },
            { LogLevel.Debug, "    [DEBUG]         " },
            { LogLevel.Progress, " [PROGRESS] " }
        };
    }
}
