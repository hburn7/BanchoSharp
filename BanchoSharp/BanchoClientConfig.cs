using BanchoSharp.Interfaces;

namespace BanchoSharp;

public class IrcCredentials : IIrcCredentials
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
	/// ignoring QUIT, PART, JOIN, MODE, PING and some other more spammy commands sent by the server.
	/// </summary>
	/// <param name="credentials"></param>
	/// <param name="logLevel"></param>
	/// <param name="host">Should either be irc.ppy.sh or cho.ppy.sh</param>
	/// <param name="port">The port to connect to. You probably will never need to change this</param>
	public BanchoClientConfig(IIrcCredentials credentials, LogLevel logLevel = LogLevel.Info,
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

	public IIrcCredentials Credentials { get; }
	public string Host { get; }
	public int Port { get; }
	public string[]? IgnoredCommands { get; set; } =
	{
		"QUIT", "PART", "JOIN", "MODE", "PING", "366", "353", "333"
	};
}