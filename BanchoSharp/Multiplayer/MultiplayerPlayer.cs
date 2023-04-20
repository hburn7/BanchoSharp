using BanchoSharp.Interfaces;

namespace BanchoSharp.Multiplayer;

public enum TeamColor
{
	Red,
	Blue,
	None
}

public class MultiplayerPlayer : IMultiplayerPlayer
{
	public MultiplayerPlayer(IMultiplayerLobby lobby, string name, int slot, TeamColor team = TeamColor.None,
		Mods mods = Mods.None)
	{
		Lobby = lobby;
		Name = name;
		Slot = slot;
		Team = team;
		Mods = mods;
		State = PlayerState.NotReady;
		ScoreHistory = new List<IMultiplayerScoreReport>();
	}

	public int? Id { get; set; }
	public string Name { get; }
	public TeamColor Team { get; set; }
	public int Slot { get; set; }

	// The mods the player is using, these only get updated after "!mp settings" has been ran.
	public Mods Mods { get; set; }
	public List<IMultiplayerScoreReport> ScoreHistory { get; }
	[Obsolete("IsReady is deprecated, use PlayerState instead", true)]
	public bool IsReady { get; set; }
	public PlayerState State { get; set; }
	public IMultiplayerLobby? Lobby { get; set; }
	public string TargetableName() => Id.HasValue ? $"#{Id}" : Name.Replace(' ', '_');
	public void AddScoreReport(IMultiplayerScoreReport report) => ScoreHistory.Add(report);
	public bool RemoveScoreReport(IMultiplayerScoreReport report) => ScoreHistory.Remove(report);

	public override bool Equals(object? other) => other?.GetType() == typeof(MultiplayerPlayer) &&
	                                              Name.Equals((other as MultiplayerPlayer)!.Name);

	public override int GetHashCode() => Name.GetHashCode();
	public bool Equals(MultiplayerPlayer other) => Name.Equals(other.Name);
}