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
	public event Action? OnAuthenticated;
	public event Action? OnAuthenticationFailed;
	public event Action<IChatMessage> OnMessageReceived;
	public event Action<IDirectMessage> OnDirectMessageReceived;
	public event Action<string>? OnDeploy;
	public event Action<string>? OnChannelSuccessfullyConnected;
	public event Action<string>? OnChannelParted;
	public event Action<string>? OnUserQueried;
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
	
		// todo: check for connection error

		OnChannelSuccessfullyConnected?.Invoke(name);
		
		// todo: channels.add(...);
	}

	public async Task PartChannelAsync(string name)
	{
		await Execute($"PART {name}");
		OnChannelParted?.Invoke(name);
	}

	public async Task QueryUserAsync(string user)
	{
		await Execute($"QUERY {user}");
		OnUserQueried?.Invoke(user);
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
				message = new DirectChatMessage(line);
			}

			OnMessageReceived?.Invoke(message);

			if (message is IDirectMessage dm)
			{
				OnDirectMessageReceived?.Invoke(dm);
			}
		}
	}
}