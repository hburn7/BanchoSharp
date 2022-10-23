using BanchoSharp.Interfaces;

namespace BanchoSharp.Messaging;

public class Channel : IChatChannel
{
	public string FullName { get; }
	public LinkedList<IIrcMessage>? MessageHistory { get; }
	public DateTime CreatedAt { get; }

	/// <summary>
	/// A Channel object keeps track of some basic information with respect to
	/// IRC channels and optionally stores all messages sent to and from them.
	/// </summary>
	/// <param name="fullName">The full name of this channel. e.g. #english.
	/// Can also be the name of a user.</param>
	public Channel(string fullName)
	{
		FullName = fullName;
		MessageHistory = new LinkedList<IIrcMessage>();
		CreatedAt = DateTime.Now;
	}

	public override string ToString() => FullName;
}