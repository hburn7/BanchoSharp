namespace BanchoSharp.Multiplayer;

public enum TeamColor
{
	Red,
	Blue,
	None
}

public class MultiplayerPlayer
{
	public int? Id { get; set; }
	
	public string Name { get; }
	public TeamColor Team { get; set; }
	public int Slot { get; set; }
	
	// The mods the player is using, these only get updated after "!mp settings" has been ran.
	public Mods Mods { get; set; }

	public int? Score { get; set; }
	public bool? Passed { get; set; }

	public MultiplayerPlayer(string name, int slot, TeamColor team = TeamColor.None, Mods mods = Mods.None)
	{
		Name = name;
		Slot = slot;
		Team = team;
		Mods = mods;
	}

	public override bool Equals(object? other) => other?.GetType() == typeof(MultiplayerPlayer) && 
	                                              this.Name.Equals((other as MultiplayerPlayer)!.Name);

	public override int GetHashCode() => Name.GetHashCode();
	public bool Equals(MultiplayerPlayer other) => this.Name.Equals(other.Name);
	public static bool operator ==(MultiplayerPlayer p1, MultiplayerPlayer p2) => p1.Name.Equals(p2.Name);
	public static bool operator !=(MultiplayerPlayer p1, MultiplayerPlayer p2) => !(p1 == p2);
	public static bool operator ==(MultiplayerPlayer p1, string name2) => p1.Name.Equals(name2);
	public static bool operator !=(MultiplayerPlayer p1, string name2) => !p1.Name.Equals(name2);
}