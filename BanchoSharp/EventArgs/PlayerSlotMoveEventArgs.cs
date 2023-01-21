using BanchoSharp.Interfaces;

namespace BanchoSharp.EventArgs;

public class PlayerSlotMoveEventArgs : System.EventArgs
{
	public PlayerSlotMoveEventArgs(IMultiplayerPlayer player, int previousSlot, int newSlot)
	{
		Player = player;
		PreviousSlot = previousSlot;
		NewSlot = newSlot;
	}

	public IMultiplayerPlayer Player { get; }
	public int PreviousSlot { get; }
	public int NewSlot { get; }
}