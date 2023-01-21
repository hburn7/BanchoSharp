using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;

namespace BanchoSharp.EventArgs;

public class PlayerChangedTeamEventArgs : System.EventArgs
{
	public PlayerChangedTeamEventArgs(IMultiplayerPlayer player, TeamColor previousTeam)
	{
		Player = player;
		PreviousTeam = previousTeam;
	}

	public IMultiplayerPlayer Player { get; }
	public TeamColor PreviousTeam { get; }
}