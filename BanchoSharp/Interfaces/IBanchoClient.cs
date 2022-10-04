namespace BanchoSharp.Interfaces;

public interface IBanchoClient
{
	/// <summary>
	/// Fired whenever the client is connected
	/// </summary>
	public event Action OnConnected;
	/// <summary>
	/// Fired whenever the client is disconnected
	/// </summary>
	public event Action OnDisconnected;

	/// <summary>
	/// Fired when the client successfully logs into Bancho with correct credentials
	/// </summary>
	public event Action OnAuthenticated;
	/// <summary>
	/// Fired whenever the client failed to authenticate with the server
	/// </summary>
	public event Action OnAuthenticationFailed;

	/// <summary>
	/// Fired whenever a message is received by the client
	/// </summary>
	public event Action<IChatMessage> OnMessageReceived;
	/// <summary>
	/// Fired whenever a PrivateMessage is received. Private messages
	/// are not always private. Any chat sent between users or to any public chat
	/// channel is denoted as a private message. To listen to strictly private messages
	/// between users, listen to <see cref="OnAuthenticatedUserDMReceived"/>
	/// </summary>
	public event Action<IPrivateMessage> OnPrivateMessageReceived;
	/// <summary>
	/// Fired whenever the authenticated user gets a new incoming direct message
	/// from another user.
	/// </summary>
	public event Action<IPrivateMessage> OnAuthenticatedUserDMReceived;
	/// <summary>
	/// Fired whenever the library sends a message directly to the server.
	/// </summary>
	public event Action<string> OnDeploy;

	/// <summary>
	/// Fired when the client failes to connect to a channel
	/// </summary>
	public event Action<string> OnChannelJoinFailure;
	/// <summary>
	/// Fired when the client has parted from a channel
	/// </summary>
	public event Action<string> OnChannelParted;
	/// <summary>
	/// Fired when a query is called on a user
	/// </summary>
	public event Action<string> OnUserQueried;
	
	/// <summary>
	/// A list of channels that the client is currently connected to
	/// </summary>
	public List<string> Channels { get; }
	
	/// <summary>
	/// Whether the client is currently connected
	/// </summary>
	public bool IsConnected { get; }
	/// <summary>
	/// Whether the client is currently connected under
	/// authorized credentials
	/// </summary>
	public bool IsAuthenticated { get; }
	/// <summary>
	/// Initiates a connection to Bancho
	/// </summary>
	/// <returns></returns>
	public Task ConnectAsync();
	/// <summary>
	/// Disconnects the client from Bancho
	/// </summary>
	/// <returns></returns>
	public Task DisconnectAsync();
	/// <summary>
	/// Sends the desired message to the server
	/// </summary>
	/// <param name="message">Raw IRC message</param>
	/// <returns></returns>
	public Task SendAsync(string message);
	/// <summary>
	/// Sends a private message to the destination with
	/// the content.
	/// </summary>
	/// <param name="destination">The channel or user to send to</param>
	/// <param name="content">The text to send</param>
	/// <returns></returns>
	public Task SendAsync(string destination, string content);

	/// <summary>
	/// Connects to the given channel. Channel names must start with "#"
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public Task JoinChannelAsync(string name);
	/// <summary>
	/// Disconnects from the given channel. Also works for direct messages
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public Task PartChannelAsync(string name);
	/// <summary>
	/// Initiates a line of communication with another user
	/// </summary>
	/// <param name="user"></param>
	/// <returns></returns>
	public Task QueryUserAsync(string user);
	
	//todo: add commands like make, makeprivate, etc.
}