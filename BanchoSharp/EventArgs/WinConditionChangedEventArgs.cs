using BanchoSharp.Multiplayer;

namespace BanchoSharp.EventArgs;

/// <summary>
/// EventArgs class for the WinConditionChanged event.
/// </summary>
public class WinConditionChangedEventArgs : System.EventArgs
{
	/// <summary>
	/// The previous win condition.
	/// </summary>
	public WinCondition PreviousWinCondition { get; set; }

	/// <summary>
	/// The current win condition.
	/// </summary>
	public WinCondition CurrentWinCondition { get; set; }

	/// <summary>
	/// Initializes a new instance of the WinConditionChangedEventArgs class.
	/// </summary>
	/// <param name="previous">The previous win condition.</param>
	/// <param name="current">The current win condition.</param>
	public WinConditionChangedEventArgs(WinCondition previous, WinCondition current)
	{
		PreviousWinCondition = previous;
		CurrentWinCondition = current;
	}
}