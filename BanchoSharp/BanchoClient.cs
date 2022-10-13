using BanchoSharp.Exceptions;
using BanchoSharp.Interfaces;
using BanchoSharp.Messaging;
using BanchoSharp.Messaging.ChatMessages;
using System.Net.Sockets;

namespace BanchoSharp;

public class BanchoClient : IBanchoClient
{
	private StreamReader? _reader;
	private TcpClient? _tcp;
	private StreamWriter? _writer;
	/// <summary>
	///  Initializes a new <see cref="BanchoClient" /> which allows for connecting
	///  to osu!Bancho's IRC server.
	/// </summary>
	/// <param name="clientConfig"></param>
#pragma warning disable CS8618
	public BanchoClient(BanchoClientConfig clientConfig)
	{
		ClientConfig = clientConfig;
		RegisterEvents();
	}
#pragma warning restore CS8618
	public BanchoClientConfig ClientConfig { get; }
	public event Action OnConnected;
	public event Action OnDisconnected;
	public event Action OnAuthenticated;
	public event Action OnAuthenticationFailed;
	public event Action<IIrcMessage> OnMessageReceived;
	public event Action<IPrivateIrcMessage> OnPrivateMessageReceived;
	public event Action<IPrivateIrcMessage> OnAuthenticatedUserDMReceived;
	public event Action<string> OnDeploy;
	public event Action<IChatChannel>? OnChannelJoined;
	public event Action<string>? OnChannelJoinFailure;
	public event Action<IChatChannel> OnChannelParted;
	public event Action<string> OnUserQueried;
	public IList<IChatChannel> Channels { get; } = new List<IChatChannel>();
	public bool IsConnected => _tcp?.Connected ?? false;
	public bool IsAuthenticated { get; private set; }

	public async Task ConnectAsync()
	{
		if (IsConnected)
		{
			return;
		}

		_tcp = new TcpClient(ClientConfig.Host, ClientConfig.Port);
		OnConnected?.Invoke();

		var ns = _tcp.GetStream();
		_reader = new StreamReader(ns);
		_writer = new StreamWriter(ns)
		{
			NewLine = "\r\n",
			AutoFlush = true
		};

		await Execute($"PASS {ClientConfig.Credentials.Password}");
		await Execute($"NICK {ClientConfig.Credentials.Username}");
		await Execute($"USER {ClientConfig.Credentials.Username}");

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
	public async Task SendPrivateMessageAsync(string destination, string content) => await Execute($"PRIVMSG {destination} {content}");

	public async Task JoinChannelAsync(string name)
	{
		await Execute($"JOIN {name}");
		var channel = new Channel(name);
		Channels.Add(channel);
		OnChannelJoined?.Invoke(channel);
	}

	public async Task PartChannelAsync(string name)
	{
		await Execute($"PART {name}");
		var channel = Channels.FirstOrDefault(x => x.FullName == name);

		if (channel == null)
		{
			Logger.Warn($"Requested removal of channel {name} but a match was not found in the channels list.");
			return;
		}
		
		Channels.Remove(channel);
		OnChannelParted?.Invoke(channel);
	}

	public async Task QueryUserAsync(string user)
	{
		await Execute($"QUERY {user}");
		Channels.Add(new Channel(user));
		OnUserQueried?.Invoke(user);
	}

	public async Task MakeTournamentLobbyAsync(string name, bool isPrivate = false)
	{
		if (!Channels.Any(x => x.FullName == "BanchoBot"))
		{
			await JoinChannelAsync("BanchoBot");
		}

		string arg = isPrivate ? "makeprivate" : "make";
		await SendPrivateMessageAsync("BanchoBot", $"!mp {arg} {name}");

		// todo: join the channel sent by banchobot
	}

	private void RegisterEvents()
	{
		OnConnected += () => Logger.Info("Client connected");
		OnDisconnected += () => Logger.Info("Client disconnected");
		OnAuthenticated += () => Logger.Info("Authenticated with osu!Bancho successfully");
		OnAuthenticationFailed += () => Logger.Warn("Failed to authenticate with osu!Bancho (invalid credentials)");
		OnMessageReceived += m => Logger.Debug($"Message received: {m}");
		OnDeploy += s => Logger.Debug($"Deployed message to osu!Bancho: {s}");
		OnChannelJoinFailure += c => Logger.Info($"Failed to join channel {c}");
		OnChannelParted += c => Logger.Info($"Parted {c}");
		OnUserQueried += u => Logger.Info($"Querying {u}");

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
			var match = Channels.FirstOrDefault(x => x.FullName == name);
			if (match != null)
			{
				Channels.Remove(match);
			}
			Logger.Info($"Failed to connect to channel {name}");
		};

		OnAuthenticated += () => this.IsAuthenticated = true;
		OnDisconnected += () => this.IsAuthenticated = false;
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

			IIrcMessage message = new IrcMessage(line);
			if (ClientConfig.IgnoredCommands?.Any(x => x.ToString().Equals(message.Command)) ?? false)
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
					throw new IrcClientNotAuthenticatedException("Authentication failure");
				}

				if (message.Command == "001")
				{
					// Message of the day received
					OnAuthenticated?.Invoke();
				}
			}

			if (message.Command == "PRIVMSG")
			{
				message = new PrivateIrcMessage(line);
			}

			OnMessageReceived?.Invoke(message);

			if (message is IPrivateIrcMessage dm)
			{
				OnPrivateMessageReceived?.Invoke(dm);

				if (dm.Recipient == ClientConfig.Credentials.Username)
				{
					OnAuthenticatedUserDMReceived?.Invoke(dm);
				}
			}
		}
	}

	/// <summary>
	///  Performs various automations, such as joining new tournament channels
	///  upon receiving notification of their creation. Assumes messages
	///  are sent by BanchoBot
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