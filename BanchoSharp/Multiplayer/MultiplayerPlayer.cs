namespace BanchoSharp.Multiplayer;

public enum TeamColor
{
	Red,
	Blue,
	None
}

// todo: Track player mods

public class MultiplayerPlayer
{
	public string Name { get; }
	public TeamColor Team { get; }

	public MultiplayerPlayer(string name, TeamColor team = TeamColor.None)
	{
		Name = name;
		Team = team;
	}
}