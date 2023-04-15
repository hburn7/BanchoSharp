using BanchoSharp.Interfaces;

namespace BanchoSharp.EventArgs;

public class PlayerBannedEventArgs : System.EventArgs
{
	public PlayerBannedEventArgs(IMultiplayerPlayer player, DateTime banTime)
	{
		Player = player;
		BanTime = banTime;
	}

	public IMultiplayerPlayer Player { get; }
	public DateTime BanTime { get; }
}