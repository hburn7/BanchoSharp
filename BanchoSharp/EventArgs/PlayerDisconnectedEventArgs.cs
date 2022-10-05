using BanchoSharp.Multiplayer;

namespace BanchoSharp.EventArgs;

public class PlayerDisconnectedEventArgs : System.EventArgs
{
	public PlayerDisconnectedEventArgs(MultiplayerPlayer player, DateTime disconnectedAt)
	{
		Player = player;
		DisconnectedAt = disconnectedAt;
	}

	public MultiplayerPlayer Player { get; }
	public DateTime DisconnectedAt { get; }
}