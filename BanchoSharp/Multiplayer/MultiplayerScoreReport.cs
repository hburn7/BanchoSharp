using BanchoSharp.Interfaces;

namespace BanchoSharp.Multiplayer;

public class MultiplayerScoreReport : IMultiplayerScoreReport
{
	public MultiplayerScoreReport(IMultiplayerPlayer player, int score, bool passed, DateTime timestamp)
	{
		Player = player;
		Score = score;
		Passed = passed;
		Timestamp = timestamp;
	}
	
	public IMultiplayerPlayer Player { get; }
	public int Score { get; }
	public bool Passed { get; }
	public DateTime Timestamp { get; }
}