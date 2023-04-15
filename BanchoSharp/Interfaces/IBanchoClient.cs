namespace BanchoSharp.Interfaces;

/// <summary>
/// A client used to connect with osu!Bancho. Provides a number of notifications in the form
/// of events. Subscribe to any event to execute a function whenever they occur. There cannot be
/// more than 4 simultaneous connections to osu!Bancho at any point, even on different clients.
/// </summary>
public interface IBanchoClient : IDisposable
{
    /// <summary>
    /// Gets or sets the configuration of the client
    /// </summary>
    public BanchoClientConfig ClientConfig { get; set; }

    /// <summary>
    /// Interface responsible for processing events related to bancho bot.
    /// </summary>
    public IBanchoBotEvents BanchoBotEvents { get; }

    /// <summary>
    /// Gets the list of channels that client is currently connected to
    /// </summary>
    public IList<IChatChannel> Channels { get; }

    /// <summary>
    /// Indicates whether the client is currently connected
    /// </summary>
    public bool IsConnected { get; }

    /// <summary>
    /// Indicates whether the client is currently connected under authorized credentials
    /// </summary>
    public bool IsAuthenticated { get; }

    /// <summary>
    /// Event fired when the client is connected
    /// </summary>
    public event Action OnConnected;

    /// <summary>
    /// Event fired when the client is disconnected
    /// </summary>
    public event Action OnDisconnected;

    /// <summary>
    /// Event fired when the client successfully logs into Bancho with correct credentials
    /// </summary>
    public event Action OnAuthenticated;

    /// <summary>
    /// Event fired whenever the client failed to authenticate with the server
    /// </summary>
    public event Action OnAuthenticationFailed;

    /// <summary>
    /// Event fired whenever a message is received by the client
    /// </summary>
    public event Action<IIrcMessage> OnMessageReceived;

    /// <summary>
    /// Event fired whenever a private message is sent by the client
    /// </summary>
    public event Action<IPrivateIrcMessage> OnPrivateMessageSent;

    /// <summary>
    /// Event fired whenever a private message is received by the client
    /// </summary>
    public event Action<IPrivateIrcMessage> OnPrivateMessageReceived;

    /// <summary>
    /// Event fired whenever the authenticated user gets a new incoming direct message
    /// from another user.
    /// </summary>
    public event Action<IPrivateIrcMessage> OnAuthenticatedUserDMReceived;

    /// <summary>
    /// Event fired whenever the library sends a message directly to the server.
    /// </summary>
    public event Action<string> OnDeploy;

    /// <summary>
    /// Event fired when the client joins a channel
    /// </summary>
    public event Action<IChatChannel> OnChannelJoined;

    /// <summary>
    /// Event fired when the client fails to connect to a channel
    /// </summary>
    public event Action<string> OnChannelJoinFailure;

    /// <summary>
    /// Event fired when the client has parted from a channel
    /// </summary>
    public event Action<IChatChannel> OnChannelParted;

    /// <summary>
    /// Event fired when a query is called by the client for a user
    /// </summary>
    public event Action<string> OnUserQueried;

    /// <summary>
    /// Event fired whenever the server sends a ping to this client. PONG responses are handled by the library.
    /// </summary>
    public event Action OnPingReceived;

    /// <summary>
    /// Checks for whether the channel name exists in the list of channels
    /// </summary>
    /// <param name="channelName">Name of the channel to check</param>
    /// <returns>True if found, false otherwise</returns>
    public bool ContainsChannel(string channelName);

    /// <summary>
    /// Initiates a connection to Bancho
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ConnectAsync();

    /// <summary>
    /// Disconnects the client from Bancho
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DisconnectAsync();

    /// <summary>
    /// Sends the desired message to the server
    /// </summary>
    /// <param name="message">Raw IRC message</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SendAsync(string message);

    /// <summary>
    /// Sends a private message to the destination with the content.
    /// </summary>
    /// <param name="destination">The channel or user to send to</param>
    /// <param name="content">The text to send</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SendPrivateMessageAsync(string destination, string content);

    /// <summary>
    /// Connects to the given channel. Channel names must start with "#"
    /// </summary>
    /// <param name="name">The name of the channel to connect to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task JoinChannelAsync(string name);

    /// <summary>
    /// Disconnects from the given channel. Also works for direct messages
    /// </summary>
    /// <param name="name">The name of the channel to disconnect from.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task PartChannelAsync(string name);

    /// <summary>
    /// Initiates a line of communication with another user
    /// </summary>
    /// <param name="user">The user to initiate communication with.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task QueryUserAsync(string user);

    /// <summary>
    /// Contacts BanchoBot to create a new tournament lobby.
    /// </summary>
    /// <param name="name">The name of the lobby</param>
    /// <param name="isPrivate">Whether the multiplayer lobby is private.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task MakeTournamentLobbyAsync(string name, bool isPrivate = false);

    /// <summary>
    /// Used for testing purposes only. Invokes the OnMessageReceived event with the given message.
    /// </summary>
    /// <param name="message">The message that will be received by the client</param>
    public void SimulateMessageReceived(IIrcMessage message);

    /// <summary>
    /// Gets a chat channel with the specified full name.
    /// </summary>
    /// <param name="fullName">The full name of the channel.</param>
    /// <returns>The chat channel if it exists; otherwise, null.</returns>
    public IChatChannel? GetChannel(string fullName);
}