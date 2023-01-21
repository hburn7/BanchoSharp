using BanchoSharp.Interfaces;

namespace BanchoSharp.EventArgs;

public class PlayerDisconnectedEventArgs : System.EventArgs
{
	public PlayerDisconnectedEventArgs(IMultiplayerPlayer player, DateTime disconnectedAt)
	{
		Player = player;
		DisconnectedAt = disconnectedAt;
	}

	public IMultiplayerPlayer Player { get; }
	public DateTime DisconnectedAt { get; }
}