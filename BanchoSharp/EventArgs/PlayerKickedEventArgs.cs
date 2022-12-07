using BanchoSharp.Multiplayer;

namespace BanchoSharp.EventArgs;

public class PlayerKickedEventArgs : System.EventArgs
{
	public PlayerKickedEventArgs(MultiplayerPlayer player, DateTime kickTime)
	{
		Player = player;
		KickTime = kickTime;
	}

	public MultiplayerPlayer Player { get; }
	public DateTime KickTime { get; }
}