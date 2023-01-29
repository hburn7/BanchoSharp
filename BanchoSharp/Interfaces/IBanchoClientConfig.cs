namespace BanchoSharp.Interfaces;

/// <summary>
///  Configuration options for an <see cref="IBanchoClient" />.
/// </summary>
public interface IBanchoClientConfig
{
	/// <summary>
	///  The credentials for connecting to osu!Bancho
	/// </summary>
	public IIrcCredentials Credentials { get; set; }
	/// <summary>
	///  Whether every channel should have messages saved in memory. This can
	///  slightly increase memory overhead but is strongly recommended to be left enabled,
	///  otherwise unexpected exceptions may occur - such as when accessing an <see cref="IChatChannel" />'s
	///  <see cref="IChatChannel.MessageHistory" /> property.
	/// </summary>
	public bool SaveMessags { get; }
	/// <summary>
	///  The host of the connection. Can either be irc.ppy.sh or cho.ppy.sh
	/// </summary>
	public string Host { get; }
	/// <summary>
	///  Must be 6667, otherwise the connection will fail.
	/// </summary>
	public int Port { get; }
	/// <summary>
	///  An optional array of suppressed or ignored commands sent by the server.
	///  By default, the following IRC events are suppressed:
	///  { "QUIT", "PART", "JOIN", "MODE", "PING", "366", "353", "333" }. Set this
	///  property to null to not ignore any commands, or pass a new array to modify it.
	/// </summary>
	public string[]? IgnoredCommands { get; set; }
}