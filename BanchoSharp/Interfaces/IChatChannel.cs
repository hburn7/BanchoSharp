namespace BanchoSharp.Interfaces;

public interface IChatChannel
{
	/// <summary>
	/// The full name of the channel as known by the server
	/// </summary>
	public string ChannelName { get; }
	/// <summary>
	/// An optional stack of messages, pushed as they come in.
	/// Null if the client is configured to not save message history.
	/// </summary>
	public LinkedList<IIrcMessage>? MessageHistory { get; }
	/// <summary>
	/// The point in time at which this channel was created
	/// </summary>
	public DateTime CreatedAt { get; }
}