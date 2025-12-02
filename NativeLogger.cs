using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Native;

public enum LogType
{
    Info,
    Warning,
    Error,
    Debug
}

public static class NativeLogger
{
    // Updated Format:
    // {0} = Timestamp
    // {1} = Type Tag (Fixed Width)
    // {2} = Class Name (Padded Left -15 chars)
    // {3} = Line number (Padded to 4 chars)
    // {4} = Message
    
    internal static Action<string>? LoggingCallback { get; set; } = Console.WriteLine;
    
    public static string ModuleName { get; set; } = "NATIVE";
    
    private static void Log(string message, LogType type = LogType.Info,
        [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        // 1. Get Timestamp
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

        // 2. Extract Class Name
        var className = Path.GetFileNameWithoutExtension(filePath);

        // 3. Format the Type Tag (Normalized to same length for alignment)
        var typeTag = type switch
        {
            LogType.Info    => "[INFO]",
            LogType.Warning => "[WARN]",
            LogType.Error   => "[ERR ]",
            LogType.Debug   => "[DEBG]",
            _               => "[INFO]"
        };

        // 4. Build the string
        // Note: The padding is handled by the {index, alignment} syntax in LogFormat
        var finalLog = $"{timestamp} [{ModuleName}] {typeTag} {className}@{lineNumber}: {message}";
        
        LoggingCallback?.Invoke(finalLog);
    }

    // Shorthand helpers remain the same
    public static void Info(string msg, [CallerFilePath] string f = "", [CallerLineNumber] int lineNumber = 0) 
        => Log(msg, LogType.Info, f, lineNumber);
        
    public static void Error(string msg, [CallerFilePath] string f = "", [CallerLineNumber] int lineNumber = 0) 
        => Log(msg, LogType.Error, f, lineNumber);
        
    public static void Warn(string msg, [CallerFilePath] string f = "", [CallerLineNumber] int lineNumber = 0) 
        => Log(msg, LogType.Warning, f, lineNumber);
        
    public static void Debug(string msg, [CallerFilePath] string f = "", [CallerLineNumber] int lineNumber = 0) 
        => Log(msg, LogType.Debug, f, lineNumber);
}