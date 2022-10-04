using BanchoSharp.Interfaces;

namespace BanchoSharp.Messaging.ChatMessages;

public class PrivateChatMessage : ChatMessage, IPrivateMessage
{
	public PrivateChatMessage(string rawMessage) : base(rawMessage)
	{
		Recipient = Parameters[0];
		Content = string.Join(" ", Parameters[1..])[1..]; // The final [1..] removes the leading :
		Sender = RawMessage.Split(":")[1].Split("!")[0];
		IsDirect = !Sender.StartsWith("#");
	}

	public string Sender { get; }
	public string Content { get; }
	public string Recipient { get; }
	public bool IsDirect { get; }
}