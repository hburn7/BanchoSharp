using BanchoSharp.Exceptions;
using BanchoSharp.Interfaces;
using BanchoSharp.Messaging.ChatMessages;
using System.Net.Sockets;

namespace BanchoSharp;

public class BanchoClient : IBanchoClient
{
	private readonly BanchoClientConfig _clientConfig;
	
	private StreamReader? _reader;
	private TcpClient? _tcp;
	private StreamWriter? _writer;
	/// <summary>
	///  Initializes a new <see cref="BanchoClient" /> which allows for connecting
	///  to osu!Bancho's IRC server.
	/// </summary>
	/// <param name="clientConfig"></param>
#pragma warning disable CS8618
	public BanchoClient(BanchoClientConfig clientConfig) { _clientConfig = clientConfig; }
#pragma warning restore CS8618
	public event Action OnConnected;
	public event Action OnDisconnected;
	public event Action OnAuthenticated;
	public event Action OnAuthenticationFailed;
	public event Action<IChatMessage> OnMessageReceived;
	public event Action<IPrivateMessage> OnPrivateMessageReceived;
	public event Action<IPrivateMessage> OnAuthenticatedUserDMReceived;
	public event Action<string> OnDeploy;
	public event Action<string>? OnChannelJoinFailure;
	public event Action<string> OnChannelParted;
	public event Action<string> OnUserQueried;
	public List<string> Channels { get; } = new();
	public bool IsConnected => _tcp?.Connected ?? false;
	public bool IsAuthenticated { get; private set; } = false;

	public async Task ConnectAsync()
	{
		if (IsConnected)
		{
			return;
		}

		_tcp = new TcpClient(_clientConfig.Host, _clientConfig.Port);

		var ns = _tcp.GetStream();
		_reader = new StreamReader(ns);
		_writer = new StreamWriter(ns)
		{
			NewLine = "\r\n",
			AutoFlush = true
		};

		await Execute($"PASS {_clientConfig.Credentials.Password}");
		await Execute($"NICK {_clientConfig.Credentials.Username}");
		await Execute($"USER {_clientConfig.Credentials.Username}");

		OnConnected?.Invoke();

		OnMessageReceived += m =>
		{
			if (m.Command == "403")
			{
				string failedChannel = m.RawMessage.Split("No such channel")[1].Trim();
				OnChannelJoinFailure?.Invoke(failedChannel);
			}
		};
		
		OnChannelJoinFailure += name =>
		{
			Channels.Remove(name);
			Logger.Info($"Failed to connect to channel {name}");
		};
		
		await ListenerAsync();
	}

	public async Task DisconnectAsync()
	{
		if (!IsConnected)
		{
			return;
		}

		await Execute("QUIT");
		OnDisconnected?.Invoke();
	}

	public async Task SendAsync(string message) => await Execute(message);
	public async Task SendAsync(string destination, string content) => await Execute($"PRIVMSG {destination} {content}");

	public async Task JoinChannelAsync(string name)
	{
		await Execute($"JOIN {name}");
		Channels.Add(name);
	}

	public async Task PartChannelAsync(string name)
	{
		await Execute($"PART {name}");
		Channels.Remove(name);
		OnChannelParted?.Invoke(name);
	}

	public async Task QueryUserAsync(string user)
	{
		await Execute($"QUERY {user}");
		Channels.Add(user);
		OnUserQueried?.Invoke(user);
	}

	public async Task MakeTournamentLobbyAsync(string name, bool isPrivate = false)
	{
		if (!Channels.Contains("BanchoBot"))
		{
			await JoinChannelAsync("BanchoBot");
		}

		string arg = isPrivate ? "makeprivate" : "make";
		await SendAsync("BanchoBot", $"!mp {arg} {name}");
		
		// todo: join the channel sent by banchobot
	}

	/// <summary>
	///  Executes a message directly to the IRC server
	/// </summary>
	/// <param name="message"></param>
	private async Task Execute(string message)
	{
		if (!IsConnected)
		{
			throw new IrcClientNotConnectedException();
		}

		await _writer!.WriteLineAsync(message);
		OnDeploy?.Invoke(message);
	}
	

	private async Task ListenerAsync()
	{
		while (IsConnected)
		{
			string? line = await _reader!.ReadLineAsync();

			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}

			IChatMessage message = new ChatMessage(line);
			if (_clientConfig.IgnoredCommands?.Any(x => x.ToString().Equals(message.Command)) ?? false)
			{
				continue;
			}

			Logger.Debug(line);

			if (!IsAuthenticated)
			{
				if (message.Command == "464")
				{
					OnAuthenticationFailed?.Invoke();

					await DisconnectAsync();
					throw new IrcConnectionFailureException();
				}

				if (message.Command == "001")
				{
					// Message of the day received
					OnAuthenticated?.Invoke();
				}
			}

			if (message.Command == "PRIVMSG")
			{
				message = new PrivateChatMessage(line);
			}

			OnMessageReceived?.Invoke(message);

			if (message is IPrivateMessage dm)
			{
				OnPrivateMessageReceived?.Invoke(dm);

				if (dm.Recipient == _clientConfig.Credentials.Username)
				{
					OnAuthenticatedUserDMReceived?.Invoke(dm);
				}
			}
		}
	}

	/// <summary>
	/// Performs various automations, such as joining new tournament channels
	/// upon receiving notification of their creation. Assumes messages
	/// are sent by BanchoBot
	/// </summary>
	/// <param name="message"></param>
	private async Task ProcessBanchoBotResponseAsync(string message)
	{
		if (message.StartsWith("Created the tournament match "))
		{
			string[] splits = message.Split();
			string url = splits[4];
			int subStart = url.LastIndexOf('/');
			int id = int.Parse(url[(subStart + 1)..]);
			await JoinChannelAsync($"#mp_{id}");
		}
	}
}