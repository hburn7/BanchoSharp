using BanchoSharp.Exceptions;
using BanchoSharp.Interfaces;
using BanchoSharp.Messaging;
using BanchoSharp.Messaging.ChatMessages;
using System.Net.Sockets;

namespace BanchoSharp;

public class BanchoClient : IBanchoClient
{
	private readonly Dictionary<string, bool> _ignoredCommands;
	/// <summary>
	///  Invoker interface responsible for processing messages sent by BanchoBot.
	/// </summary>
	private IBanchoBotEventInvoker _banchoBotEventInvoker;
	private StreamReader? _reader;
	private TcpClient? _tcp;
	private StreamWriter? _writer;
	// public event Action<IMultiplayerLobby> OnMultiplayerLobbyCreated;
	public event Action? OnPingReceived;
	public BanchoClientConfig ClientConfig { get; set; }
	public IBanchoBotEvents BanchoBotEvents { get; private set; }
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

		if (Channels.Any(x => x.ChannelName.Equals(user, StringComparison.OrdinalIgnoreCase)))
		{
			return;
		}
		
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

	private void RegisterInvokers()
	{
		_banchoBotEventInvoker = new BanchoBotEventInvoker(this);
		BanchoBotEvents = (IBanchoBotEvents)_banchoBotEventInvoker;
	}

	private void RegisterEvents()
	{
		OnConnected += () => Logger.Info("Client connected");
		OnDisconnected += () =>
		{
			Logger.Info("Client disconnected, disposing.");
			Dispose();
			Logger.Info("Client disposed.");
		};
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

		OnChannelJoined += c => Logger.Info($"Joined channel {c}");
		OnChannelJoinFailure += c => Logger.Info($"Failed to join channel {c}");
		OnChannelParted += c => Logger.Info($"Parted {c}");
		OnUserQueried += u => Logger.Info($"Queried {u}");

		OnMessageReceived += async m =>
		{
			Logger.Debug($"Message received: {m}");

			if (m is IPrivateIrcMessage priv)
			{
				_banchoBotEventInvoker.ProcessMessage(priv);
				LinkedList<IIrcMessage>? messageHistory;
				if (priv.IsDirect)
				{
					messageHistory = GetChannel(priv.Sender)?.MessageHistory;
				}
				else
				{
					messageHistory = GetChannel(priv.Recipient)?.MessageHistory;
				}

				if (messageHistory == null)
				{
					Logger.Warn($"Failed to append to MessageHistory for {priv}");
				}
				else
				{
					messageHistory.AddLast(priv);
				}
			}

			if (m.Command == "332")
			{
				// sample message: "#mp_105079765 :multiplayer game #4464"
				string channelNameMessage = m.RawMessage.Split(" :")[0];
				string channelName = channelNameMessage[channelNameMessage.IndexOf("#mp_", StringComparison.Ordinal)..];

				var channel = new Channel(channelName);

				if (Channels.Any(x => x.ChannelName.Equals(channel.ChannelName, StringComparison.OrdinalIgnoreCase)))
				{
					return;
				}

				Channels.Add(channel);
				OnChannelJoined?.Invoke(channel);
			}
			else if (m.Command == "403")
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
			Logger.Info($"Failed to connect to channel {name}");
		};

		OnAuthenticated += () => IsAuthenticated = true;
		OnDisconnected += () => IsAuthenticated = false;
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
			if (_ignoredCommands?.ContainsKey(message.Command) ?? false)
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
	/// <param name="clientConfig">The configuration of this client</param>
#pragma warning disable CS8618
	public BanchoClient(BanchoClientConfig clientConfig)
	{
		ClientConfig = clientConfig;

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

		RegisterInvokers();
		RegisterEvents();
	}

	public BanchoClient()
	{
		ClientConfig = new BanchoClientConfig(new IrcCredentials());

		_banchoBotEventInvoker = new BanchoBotEventInvoker(this);
		BanchoBotEvents = (IBanchoBotEvents)_banchoBotEventInvoker;

		RegisterInvokers();
		RegisterEvents();
	}
#pragma warning restore CS8618
	protected virtual void Dispose(bool disposing)
	{
		// In the event we inherit from this class, if necessary, this would be overridden
		// and further resources should be disposed.
		if (disposing)
		{
			_reader?.Dispose();
			_tcp?.Dispose();
			_writer?.Dispose();

			if (IsConnected)
			{
				DisconnectAsync().GetAwaiter().GetResult();
			}
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}