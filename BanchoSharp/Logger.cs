namespace BanchoSharp;

/// <summary>
///  Enum to hold log level flag.
/// </summary>
public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warn,
    Error,
    None
}

/// <summary>
/// The Logger class provides a simple logging mechanism for the application 
/// </summary>
public static class Logger
{
    /// <summary>
    /// Gets or sets the current log level used by the Logger.
    /// </summary>
    public static LogLevel LogLevel { get; set; }

    /// <summary>
    /// Logs an object with the LogLevel Trace.
    /// </summary>
    /// <param name="value">The object to log.</param>
    public static void Trace(object value) => Log(LogLevel.Trace, value);

    /// <summary>
    /// Logs an object with the LogLevel Debug.
    /// </summary>
    /// <param name="value">The object to log.</param>
    public static void Debug(object value) => Log(LogLevel.Debug, value);

    /// <summary>
    /// Logs an object with the LogLevel Info.
    /// </summary>
    /// <param name="value">The object to log.</param>
    public static void Info(object value) => Log(LogLevel.Info, value);

    /// <summary>
    /// Logs an object with the LogLevel Warn.
    /// </summary>
    /// <param name="value">The object to log.</param>
    public static void Warn(object value) => Log(LogLevel.Warn, value);

    /// <summary>
    /// Logs an object with the LogLevel Error.
    /// </summary>
    /// <param name="value">The object to log.</param>
    public static void Error(object value) => Log(LogLevel.Error, value);

    /// <summary>
    /// Logs an object with the specified log level.
    /// </summary>
    /// <param name="level">The LogLevel to use for this message.</param>
    /// <param name="value">The object to log.</param>
    private static void Log(LogLevel level, object value)
    {
        if (LogLevel == LogLevel.None)
        {
            return;
        }

        if (level < LogLevel)
        {
            return;
        }

        var defaultForeground = Console.ForegroundColor;

        Console.ForegroundColor = level switch
        {
            LogLevel.Trace => ConsoleColor.DarkGray,
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Warn => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            _ => Console.ForegroundColor
        };

        Console.WriteLine($"{DateTime.Now:HH:mm:ss} [{level}]: {value}");
        Console.ForegroundColor = defaultForeground;
    }
}