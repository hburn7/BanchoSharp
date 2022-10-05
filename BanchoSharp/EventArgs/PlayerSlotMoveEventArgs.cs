using BanchoSharp.Multiplayer;

namespace BanchoSharp.EventArgs;

public class PlayerSlotMoveEventArgs : System.EventArgs
{
	public PlayerSlotMoveEventArgs(MultiplayerPlayer player, int previousSlot, int newSlot)
	{
		Player = player;
		PreviousSlot = previousSlot;
		NewSlot = newSlot;
	}

	public MultiplayerPlayer Player { get; }
	public int PreviousSlot { get; }
	public int NewSlot { get; }
}