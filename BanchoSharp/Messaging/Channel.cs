using BanchoSharp.Interfaces;

namespace BanchoSharp.Messaging;

public class Channel : IChatChannel
{
	public string ChannelName { get; }
	public LinkedList<IIrcMessage>? MessageHistory { get; }
	public DateTime CreatedAt { get; }

	/// <summary>
	/// A Channel object keeps track of some basic information with respect to
	/// IRC channels and optionally stores all messages sent to and from them.
	/// </summary>
	/// <param name="fullName">The full name of this channel. e.g. #english.
	/// Can also be the name of a user.</param>
	/// <param name="saveMessages">Whether the client needs to save the message history in memory</param>
	public Channel(string fullName, bool saveMessages)
	{
		ChannelName = fullName;
		MessageHistory = saveMessages ? new LinkedList<IIrcMessage>() : null;
		CreatedAt = DateTime.Now;
	}

	public override string ToString() => ChannelName;
}