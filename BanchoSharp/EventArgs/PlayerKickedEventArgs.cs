using BanchoSharp.Interfaces;

namespace BanchoSharp.EventArgs;

public class PlayerKickedEventArgs : System.EventArgs
{
	public PlayerKickedEventArgs(IMultiplayerPlayer player, DateTime kickTime)
	{
		Player = player;
		KickTime = kickTime;
	}

	public IMultiplayerPlayer Player { get; }
	public DateTime KickTime { get; }
}