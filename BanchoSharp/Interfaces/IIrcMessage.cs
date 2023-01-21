namespace BanchoSharp.Interfaces;

public interface IIrcMessage
{
	/// <summary>
	///  Raw data as sent from the server
	/// </summary>
	public string RawMessage { get; }
	/// <summary>
	///  The message's command parameters
	/// </summary>
	public IList<string> Parameters { get; }
	/// <summary>
	///  IRC V3.2 tags (not really relevant to this library, but maybe one day
	///  Bancho moves to V3.2)
	/// </summary>
	public IDictionary<string, string> Tags { get; }
	/// <summary>
	///  The IRC command of this message
	/// </summary>
	public string Command { get; }
	/// <summary>
	///  The message prefix. A prefix characterizes a different
	///  channel type. See here for more info: http://www.faqs.org/rfcs/rfc2812.html
	/// </summary>
	public string Prefix { get; }
	/// <summary>
	///  The moment in time for which this message was created
	/// </summary>
	public DateTime Timestamp { get; }
}