using BanchoSharp.Interfaces;

namespace BanchoSharp;

public class IrcCredentials : IIrcCredentials
{
	public IrcCredentials(string username, string password)
	{
		Username = username;
		Password = password;
	}

	public IrcCredentials()
	{
		Username = string.Empty;
		Password = string.Empty;
	}

	public string Username { get; set; }
	public string Password { get; set; }
}

public class BanchoClientConfig : IBanchoClientConfig
{
	public BanchoClientConfig(IIrcCredentials credentials, LogLevel logLevel = LogLevel.Info,
		bool saveMessags = true, string host = "irc.ppy.sh", int port = 6667, bool isBot = false)
	{
		Credentials = credentials;
		SaveMessags = saveMessags;
		Host = host;
		Port = port;
		IsBot = isBot;

		Logger.LogLevel = logLevel;
	}

	public BanchoClientConfig(IIrcCredentials credentials, string[]? ignoredCommands,
		bool saveMessages = true, LogLevel logLevel = LogLevel.Info, string host = "irc.ppy.sh", int port = 6667,
		bool isBot = false)
		: this(credentials, logLevel, saveMessages, host, port, isBot)
	{
		IgnoredCommands = ignoredCommands;
	}

	public IIrcCredentials Credentials { get; set; }
	public bool SaveMessags { get; }
	public string Host { get; }
	public int Port { get; }
	public bool IsBot { get; }
	public string[]? IgnoredCommands { get; set; } =
	{
		"QUIT", "PART", "JOIN", "MODE", "366", "353", "333"
	};
}