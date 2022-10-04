namespace BanchoSharp;

public class IrcCredentials
{
	public IrcCredentials(string username, string password)
	{
		Username = username;
		Password = password;
	}

	public string Username { get; }
	public string Password { get; }
}

public class BanchoClientConfig
{
	/// <summary>
	/// Constructs the configuration that is used by the BanchoClient. Defaults to
	/// ignoring QUIT, PART, JOIN, MODE, and PING commands sent by the server.
	/// </summary>
	/// <param name="credentials"></param>
	/// <param name="logLevel"></param>
	/// <param name="host">Should either be irc.ppy.sh or cho.ppy.sh</param>
	/// <param name="port">The port to connect to. You probably will never need to change this</param>
	public BanchoClientConfig(IrcCredentials credentials, LogLevel logLevel = LogLevel.Info,
		string host = "irc.ppy.sh", int port = 6667)
	{
		Credentials = credentials;
		Host = host;
		Port = port;

		Logger.LogLevel = logLevel;
	}

	public BanchoClientConfig(IrcCredentials credentials, string[]? ignoredCommands,
		LogLevel logLevel = LogLevel.Info, string host = "irc.ppy.sh", int port = 6667)
		: this(credentials, logLevel, host, port)
	{
		IgnoredCommands = ignoredCommands;
	}

	public IrcCredentials Credentials { get; }
	public string Host { get; }
	public int Port { get; }
	public string[]? IgnoredCommands { get; set; } =
	{
		"QUIT", "PART", "JOIN", "MODE", "PING"
	};
}