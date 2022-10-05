using BanchoSharp.Multiplayer;

namespace BanchoSharp.EventArgs;

public class PlayerChangedTeamEventArgs : System.EventArgs
{
	public PlayerChangedTeamEventArgs(MultiplayerPlayer player, TeamColor previousTeam)
	{
		Player = player;
		PreviousTeam = previousTeam;
	}

	public MultiplayerPlayer Player { get; }
	public TeamColor PreviousTeam { get; }
}