namespace BanchoSharp.Interfaces;

/// <summary>
///  A client used to connect with osu!Bancho. Provides a number of notifications in the form
///  of events. Subscribe to any event to execute a function whenever they occur. There cannot be
///  more than 4 simultaneous connections to osu!Bancho at any point, even on different clients.
/// </summary>
public interface IBanchoClient : IDisposable
{
	public BanchoClientConfig ClientConfig { get; set; }
	/// <summary>
	/// Interface responsible for the processing of all events related to BanchoBot. Subscribe to this
	/// interface's events for anything related to BanchoBot.
	/// </summary>
	public IBanchoBotEvents BanchoBotEvents { get; }
	/// <summary>
	///  A list of channels that the client is currently connected to
	/// </summary>
	public IList<IChatChannel> Channels { get; }
	/// <summary>
	///  Whether the client is currently connected
	/// </summary>
	public bool IsConnected { get; }
	/// <summary>
	///  Whether the client is currently connected under
	///  authorized credentials
	/// </summary>
	public bool IsAuthenticated { get; }
	/// <summary>
	///  Fired whenever the client is connected
	/// </summary>
	public event Action OnConnected;
	/// <summary>
	///  Fired whenever the client is disconnected
	/// </summary>
	public event Action OnDisconnected;
	/// <summary>
	///  Fired when the client successfully logs into Bancho with correct credentials
	/// </summary>
	public event Action OnAuthenticated;
	/// <summary>
	///  Fired whenever the client failed to authenticate with the server
	/// </summary>
	public event Action OnAuthenticationFailed;
	/// <summary>
	///  Fired whenever a message is received by the client
	/// </summary>
	public event Action<IIrcMessage> OnMessageReceived;
	public event Action<IPrivateIrcMessage> OnPrivateMessageSent;
	/// <summary>
	///  Fired whenever a PrivateMessage is received. Private messages
	///  are not always private. Any chat sent between users or to any public chat
	///  channel is denoted as a private message. To listen to strictly private messages
	///  between users, listen to <see cref="OnAuthenticatedUserDMReceived" />
	/// </summary>
	public event Action<IPrivateIrcMessage> OnPrivateMessageReceived;
	/// <summary>
	///  Fired whenever the authenticated user gets a new incoming direct message
	///  from another user.
	/// </summary>
	public event Action<IPrivateIrcMessage> OnAuthenticatedUserDMReceived;
	/// <summary>
	///  Fired whenever the library sends a message directly to the server.
	/// </summary>
	public event Action<string> OnDeploy;
	public event Action<IChatChannel> OnChannelJoined;
	/// <summary>
	///  Fired when the client failes to connect to a channel
	/// </summary>
	public event Action<string> OnChannelJoinFailure;
	/// <summary>
	///  Fired when the client has parted from a channel
	/// </summary>
	public event Action<IChatChannel> OnChannelParted;
	/// <summary>
	///  Fired when a query is called on a user
	/// </summary>
	public event Action<string> OnUserQueried;
	/// <summary>
	///  Fired whenever the server sends a ping to this client. PONG responses are handled by the library.
	/// </summary>
	public event Action OnPingReceived;

	/// <summary>
	///  Initiates a connection to Bancho
	/// </summary>
	/// <returns></returns>
	public Task ConnectAsync();

	/// <summary>
	///  Disconnects the client from Bancho
	/// </summary>
	/// <returns></returns>
	public Task DisconnectAsync();

	/// <summary>
	///  Sends the desired message to the server
	/// </summary>
	/// <param name="message">Raw IRC message</param>
	/// <returns></returns>
	public Task SendAsync(string message);

	/// <summary>
	///  Sends a private message to the destination with
	///  the content.
	/// </summary>
	/// <param name="destination">The channel or user to send to</param>
	/// <param name="content">The text to send</param>
	/// <returns></returns>
	public Task SendPrivateMessageAsync(string destination, string content);

	/// <summary>
	///  Connects to the given channel. Channel names must start with "#"
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public Task JoinChannelAsync(string name);

	/// <summary>
	///  Disconnects from the given channel. Also works for direct messages
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public Task PartChannelAsync(string name);

	/// <summary>
	///  Initiates a line of communication with another user
	/// </summary>
	/// <param name="user"></param>
	/// <returns></returns>
	public Task QueryUserAsync(string user);

	/// <summary>
	///  Contacts BanchoBot to create a new tournament lobby.
	/// </summary>
	/// <param name="name">The name of the lobby</param>
	/// <param name="isPrivate">Whether the multiplayer lobby is private</param>
	/// <returns></returns>
	public Task MakeTournamentLobbyAsync(string name, bool isPrivate = false);

	public IChatChannel? GetChannel(string fullName);
	//todo: add commands like make, makeprivate, etc.
}