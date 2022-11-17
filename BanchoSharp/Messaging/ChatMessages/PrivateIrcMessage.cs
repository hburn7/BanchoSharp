using BanchoSharp.Interfaces;

namespace BanchoSharp.Messaging.ChatMessages;

public class PrivateIrcMessage : IrcMessage, IPrivateIrcMessage
{
	public PrivateIrcMessage(string rawMessage, string recipientUsername) : base(rawMessage)
	{
		Recipient = Parameters[0];
		Content = Parameters[1];
		Sender = Prefix.Split("!cho@ppy.sh")[0];
		
		if (Sender.StartsWith(":"))
		{
			 // The trailing [1..] removes the first colon
			 Sender = Sender[1..];
		}

		IsDirect = !Sender.StartsWith("#") && Recipient.Equals(recipientUsername, StringComparison.OrdinalIgnoreCase);
		IsBanchoBotMessage = Sender == "BanchoBot";
	}

	public string Sender { get; }
	public string Content { get; }
	public string Recipient { get; }
	public bool IsDirect { get; }
	public bool IsBanchoBotMessage { get; }

	/// <summary>
	/// Creates an <see cref="IPrivateIrcMessage"/> from basic parameters.
	/// This is handy if you need to create an instance of this class for
	/// display purposes (e.g. a user sends a message through your client implementation).
	/// </summary>
	/// <param name="sender">The sender of the message</param>
	/// <param name="recipient">The recipient of the message</param>
	/// <param name="content">The message's content</param>
	/// <param name="username">The username of the logged in user</param>
	/// <returns>Fully loaded <see cref="IPrivateIrcMessage"/></returns>
	public static IPrivateIrcMessage CreateFromParameters(string sender, string recipient, string content) =>
		new PrivateIrcMessage($":{sender}!cho@ppy.sh PRIVMSG {recipient} :{content}", recipient);
}