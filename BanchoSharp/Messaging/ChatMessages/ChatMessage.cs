using BanchoSharp.Interfaces;

namespace BanchoSharp.Messaging.ChatMessages;

public class ChatMessage : IChatMessage
{
	public string RawMessage { get; }
	public string[] Parts { get; }
	public string[] Parameters { get; }
	public string Command { get; }
	public string? Prefix { get; }
	public DateTime Timestamp { get; }

	public ChatMessage(string rawMessage)
	{
		RawMessage = rawMessage;
		Parts = rawMessage.Split();
		Command = Parts[0];

		if (Parts[0].StartsWith(":"))
		{
			Prefix = Parts[0];
			Parts = Parts[1..];
		}

		Command = Parts[0];
		Parameters = Parts[1..];
		Timestamp = DateTime.Now;
	}
}