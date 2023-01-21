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
	public int? Id { get; set; }
	public string Name { get; }
	public TeamColor Team { get; set; }
	public int Slot { get; set; }
	
	// The mods the player is using, these only get updated after "!mp settings" has been ran.
	public Mods Mods { get; set; }

	public int? Score { get; set; }
	public bool? Passed { get; set; }
	public bool IsReady { get; set; }

	public MultiplayerPlayer(IMultiplayerLobby lobby, string name, int slot, TeamColor team = TeamColor.None, Mods mods = Mods.None)
	{
		Lobby = lobby;
		Name = name;
		Slot = slot;
		Team = team;
		Mods = mods;
	}

	public IMultiplayerLobby? Lobby { get; set; }
	public string TargetableName() => Id.HasValue ? $"#{Id}" : Name;

	public override bool Equals(object? other) => other?.GetType() == typeof(MultiplayerPlayer) && 
	                                              this.Name.Equals((other as MultiplayerPlayer)!.Name);

	public override int GetHashCode() => Name.GetHashCode();
	public bool Equals(MultiplayerPlayer other) => this.Name.Equals(other.Name);
}