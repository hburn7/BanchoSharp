namespace BanchoSharp.Interfaces;

public interface IDirectMessage : IChatMessage
{
	/// <summary>
	/// Where this message is being sent from
	/// </summary>-
	public string Sender { get; }
	/// <summary>
	/// Who the message is being delivered to
	/// </summary>
	public string Recipient { get; }
	/// <summary>
	/// The relevant content of the message
	/// </summary>
	public string Content { get; }
	/// <summary>
	/// Whether the message is private between users
	/// </summary>
	public bool IsPrivate { get; }
}