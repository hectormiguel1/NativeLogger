using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Native;

public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3
}

public class NativeLogger<T>
{
    // Updated Format:
    // {0} = Timestamp
    // {1} = Type Tag (Fixed Width)
    // {2} = Class Name (Padded Left -15 chars)
    // {3} = Line number (Padded to 4 chars)
    // {4} = Message

    private readonly string _moduleName = typeof(T).Name.ToUpper();

    private void Log(string message, LogLevel level = LogLevel.Info,
        [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        //Early return if the message log level is less than  current logging level 
        if (level < LogRouter.Level)
        {
            return;
        }

        // 1. Get Timestamp
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

        // 2. Extract Class Name
        var className = Path.GetFileNameWithoutExtension(filePath);

        // 3. Format the Type Tag (Normalized to same length for alignment)
        var typeTag = level switch
        {
            LogLevel.Info => "[INFO]",
            LogLevel.Warning => "[WARN]",
            LogLevel.Error => "[ERR ]",
            LogLevel.Debug => "[DEBG]",
            _ => "[INFO]"
        };

        // 4. Build the string
        // Note: The padding is handled by the {index, alignment} syntax in LogFormat
        var finalLog = $"{timestamp} [{_moduleName}] {typeTag} {className}@{lineNumber}: {message}";

        LogRouter.GlobalCallback?.Invoke(finalLog);
    }

    // Shorthand helpers remain the same
    public void Info(string msg, [CallerFilePath] string f = "", [CallerLineNumber] int lineNumber = 0)
        => Log(msg, LogLevel.Info, f, lineNumber);

    public void Error(string msg, [CallerFilePath] string f = "", [CallerLineNumber] int lineNumber = 0)
        => Log(msg, LogLevel.Error, f, lineNumber);

    public void Warn(string msg, [CallerFilePath] string f = "", [CallerLineNumber] int lineNumber = 0)
        => Log(msg, LogLevel.Warning, f, lineNumber);

    public void Debug(string msg, [CallerFilePath] string f = "", [CallerLineNumber] int lineNumber = 0)
        => Log(msg, LogLevel.Debug, f, lineNumber);
}