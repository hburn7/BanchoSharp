using BanchoSharp.Multiplayer;

namespace BanchoSharp.EventArgs;

/// <summary>
/// EventArgs class for the GameModeChanged event.
/// </summary>
public class GameModeChangedEventArgs : System.EventArgs
{
	/// <summary>
	/// The previous game mode.
	/// </summary>
	public GameMode PreviousGameMode { get; set; }

	/// <summary>
	/// The current game mode.
	/// </summary>
	public GameMode CurrentGameMode { get; set; }

	/// <summary>
	/// Initializes a new instance of the GameModeChangedEventArgs class.
	/// </summary>
	/// <param name="previous">The previous game mode.</param>
	/// <param name="current">The current game mode.</param>
	public GameModeChangedEventArgs(GameMode previous, GameMode current)
	{
		PreviousGameMode = previous;
		CurrentGameMode = current;
	}
}