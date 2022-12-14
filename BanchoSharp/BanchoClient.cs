using BanchoSharp.Exceptions;
using BanchoSharp.Interfaces;
using BanchoSharp.Messaging;
using BanchoSharp.Messaging.ChatMessages;
using System.Net.Sockets;

namespace BanchoSharp;

public class BanchoClient : IBanchoClient
{
	/// <summary>
	///  Invoker interface responsible for processing messages sent by BanchoBot.
	/// </summary>
	private readonly IBanchoBotEventInvoker _banchoBotEventInvoker;
	private readonly Dictionary<string, bool> _ignoredCommands;
	private StreamReader? _reader;
	private TcpClient? _tcp;
	private StreamWriter? _writer;
	// public event Action<IMultiplayerLobby> OnMultiplayerLobbyCreated;
	public event Action? OnPingReceived;
	public BanchoClientConfig ClientConfig { get; }
	public IBanchoBotEvents BanchoBotEvents { get; }
	public event Action OnConnected;
	public event Action OnDisconnected;
	public event Action OnAuthenticated;
	public event Action OnAuthenticationFailed;
	public event Action<IIrcMessage> OnMessageReceived;
	public event Action<IPrivateIrcMessage>? OnPrivateMessageSent;
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

	public async Task SendPrivateMessageAsync(string destination, string content)
	{
		await Execute($"PRIVMSG {destination} {content}");
		var priv = PrivateIrcMessage.CreateFromParameters(ClientConfig.Credentials.Username, destination, content);
		Channels.FirstOrDefault(x => x.ChannelName.Equals(destination, StringComparison.OrdinalIgnoreCase))?.MessageHistory?.AddLast(priv);
		OnPrivateMessageSent?.Invoke(priv);
	}

	public async Task JoinChannelAsync(string name)
	{
		await Execute($"JOIN {name}");
		var channel = new Channel(name);

		if (Channels.Contains(channel))
		{
			return;
		}

		Channels.Add(channel);
		OnChannelJoined?.Invoke(channel);
	}

	public async Task PartChannelAsync(string name)
	{
		await Execute($"PART {name}");
		var channel = Channels.FirstOrDefault(x => x.ChannelName == name);

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
		if (!Channels.Any(x => x.ChannelName == "BanchoBot"))
		{
			await JoinChannelAsync("BanchoBot");
		}

		string arg = isPrivate ? "makeprivate" : "make";
		await SendPrivateMessageAsync("BanchoBot", $"!mp {arg} {name}");

		// todo: join the channel sent by banchobot
	}

	public IChatChannel? GetChannel(string fullName) => Channels.FirstOrDefault(x => x.ChannelName == fullName);

	private void RegisterEvents()
	{
		OnConnected += () => Logger.Info("Client connected");
		OnDisconnected += () => Logger.Info("Client disconnected");
		OnAuthenticated += () => Logger.Info("Authenticated with osu!Bancho successfully");
		OnAuthenticationFailed += () => Logger.Warn("Failed to authenticate with osu!Bancho (invalid credentials)");
		OnDeploy += s =>
		{
			if (s.StartsWith("PASS"))
			{
				// Conceal password
				s = "PASS ********";
			}

			Logger.Debug($"Deployed message to osu!Bancho: {s}");
		};

		OnChannelJoinFailure += c => Logger.Info($"Failed to join channel {c}");
		OnChannelParted += c => Logger.Info($"Parted {c}");
		OnUserQueried += u => Logger.Info($"Querying {u}");

		OnMessageReceived += async m =>
		{
			Logger.Debug($"Message received: {m}");

			if (m is IPrivateIrcMessage { IsBanchoBotMessage: true } priv)
			{
				_banchoBotEventInvoker.ProcessMessage(priv);
			}

			if (m.Command == "403")
			{
				string failedChannel = m.RawMessage.Split("No such channel")[1].Trim();
				OnChannelJoinFailure?.Invoke(failedChannel);
			}
			else if (m.Command == "PING")
			{
				Logger.Trace("Ping received");
				await Execute("PONG");
				Logger.Trace("Pong dispatched");
				OnPingReceived?.Invoke();
			}
		};

		OnChannelJoinFailure += name =>
		{
			var match = Channels.FirstOrDefault(x => x.ChannelName == name);
			if (match != null)
			{
				Channels.Remove(match);
			}

			Logger.Info($"Failed to connect to channel {name}");
		};

		OnAuthenticated += () => IsAuthenticated = true;
		OnDisconnected += () => IsAuthenticated = false;

		// BanchoBot notifications
		// OnPrivateMessageReceived += m =>
		// {
		// 	var checker = new BanchoBotChecks(this);
		// 	
		// 	// #mp_id
		// 	if (checker.IsTournamentCreation(m) is {} lobby)
		// 	{
		// 		OnMultiplayerLobbyCreated?.Invoke(lobby);
		// 	}
		// };

		// OnMultiplayerLobbyCreated += lobby => { Logger.Debug($"Multiplayer lobby created: {lobby.FullName} ({lobby.Name})"); };
	}

	/// <summary>
	///  Executes a message directly to the IRC server
	/// </summary>
	/// <param name="message"></param>
	private async Task Execute(string message)
	{
		if (!IsConnected)
		{
			Logger.Error($"IRC client not connected, failed to execute {message}");
			return;
		}

		bool bypassAuth() => message.StartsWith("NICK") || message.StartsWith("PASS") || message.StartsWith("USER");

		if (!IsAuthenticated && !bypassAuth())
		{
			throw new IrcClientNotAuthenticatedException();
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
			if (_ignoredCommands.ContainsKey(message.Command))
			{
				continue;
			}

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
				message = new PrivateIrcMessage(line, ClientConfig.Credentials.Username);
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
	///  Initializes a new <see cref="BanchoClient" /> which allows for connecting
	///  to osu!Bancho's IRC server.
	/// </summary>
	/// <param name="clientConfig"></param>
#pragma warning disable CS8618
	public BanchoClient(BanchoClientConfig clientConfig)
	{
		ClientConfig = clientConfig;
		_banchoBotEventInvoker = new BanchoBotEventInvoker(this);
		BanchoBotEvents = (IBanchoBotEvents)_banchoBotEventInvoker;

		if (ClientConfig.IgnoredCommands != null)
		{
			_ignoredCommands = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
			foreach (string cmd in ClientConfig.IgnoredCommands)
			{
				if (!_ignoredCommands.ContainsKey(cmd))
				{
					_ignoredCommands.Add(cmd, true);
				}
			}
		}

		RegisterEvents();
	}

	public BanchoClient() { ClientConfig = new BanchoClientConfig(new IrcCredentials()); }
#pragma warning restore CS8618
}