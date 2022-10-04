namespace BanchoSharp.Interfaces;

public interface IChatMessage
{
	/// <summary>
	/// Raw data as sent from the server
	/// </summary>
	public string RawMessage { get; }
	/// <summary>
	/// Parts of the message, as sent by the server,
	/// according to the IRC standard.
	/// </summary>
	public string[] Parts { get; }
	/// <summary>
	/// The message's command parameters
	/// </summary>
	public string[] Parameters { get; }
	/// <summary>
	/// The IRC command of this message
	/// </summary>
	public string Command { get; }
	
	/// <summary>
	/// The message prefix. A prefix characterizes a different
	/// channel type. See here for more info: http://www.faqs.org/rfcs/rfc2812.html
	/// </summary>
	public string? Prefix { get; }
	public DateTime Timestamp { get; }
}