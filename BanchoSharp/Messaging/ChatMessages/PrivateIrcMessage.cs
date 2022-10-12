using BanchoSharp.Interfaces;

namespace BanchoSharp.Messaging.ChatMessages;

public class PrivateIrcMessage : IrcMessage, IPrivateIrcMessage
{
	public PrivateIrcMessage(string rawMessage) : base(rawMessage)
	{
		Recipient = Parameters[0];
		Content = Parameters[1]; //string.Join(" ", Parameters[1..])[1..]; // The final [1..] removes the leading :
		Sender = Prefix.Split("!cho@ppy.sh")[0][1..]; // The trailing [1..] removes the first colon
		IsDirect = !Sender.StartsWith("#");
	}

	public string Sender { get; }
	public string Content { get; }
	public string Recipient { get; }
	public bool IsDirect { get; }
}