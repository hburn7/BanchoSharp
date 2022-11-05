namespace BanchoSharp;

public enum LogLevel
{
	Trace,
	Debug,
	Info,
	Warn,
	Error,
	None
}

public static class Logger
{
	// Set by BanchoClientConfig
	public static LogLevel LogLevel { get; set; }
	public static void Trace(object value) => Log(LogLevel.Trace, value);
	public static void Debug(object value) => Log(LogLevel.Debug, value);
	public static void Info(object value) => Log(LogLevel.Info, value);
	public static void Warn(object value) => Log(LogLevel.Warn, value);
	public static void Error(object value) => Log(LogLevel.Error, value);

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