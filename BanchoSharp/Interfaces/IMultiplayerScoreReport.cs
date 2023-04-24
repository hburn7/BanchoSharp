namespace BanchoSharp.Interfaces;

/// <summary>
/// Represents a score reported by Bancho after a match has finished (for a single player)
/// </summary>
public interface IMultiplayerScoreReport
{
	public IMultiplayerPlayer Player { get; }
	public int Score { get; }
	public bool Passed { get; }
	public DateTime Timestamp { get; }
}