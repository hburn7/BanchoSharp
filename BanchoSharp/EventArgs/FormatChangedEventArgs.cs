using BanchoSharp.Multiplayer;

namespace BanchoSharp.EventArgs;

/// <summary>
/// EventArgs class for the FormatChanged event.
/// </summary>
public class FormatChangedEventArgs : System.EventArgs
{
	/// <summary>
	/// The previous lobby format.
	/// </summary>
	public LobbyFormat PreviousLobbyFormat { get; set; }

	/// <summary>
	/// The current lobby format.
	/// </summary>
	public LobbyFormat CurrentLobbyFormat { get; set; }

	/// <summary>
	/// Initializes a new instance of the FormatChangedEventArgs class.
	/// </summary>
	/// <param name="previous">The previous lobby format.</param>
	/// <param name="current">The current lobby format.</param>
	public FormatChangedEventArgs(LobbyFormat previous, LobbyFormat current)
	{
		PreviousLobbyFormat = previous;
		CurrentLobbyFormat = current;
	}
}