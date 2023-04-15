using BanchoSharp.Interfaces;

namespace BanchoSharp.EventArgs;

/// <summary>
/// Represents the event arguments for a move in player slot.
/// </summary>
public class PlayerSlotMoveEventArgs : System.EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerSlotMoveEventArgs"/> class.
    /// </summary>
    /// <param name="player">The <see cref="IMultiplayerPlayer"/> object related to the slot move.</param>
    /// <param name="previousSlot">The previous slot number.</param>
    /// <param name="newSlot">The new slot number.</param>
    public PlayerSlotMoveEventArgs(IMultiplayerPlayer player, int previousSlot, int newSlot)
    {
        Player = player;
        PreviousSlot = previousSlot;
        NewSlot = newSlot;
    }

    /// <summary>
    /// Gets the <see cref="IMultiplayerPlayer"/> object related to the slot move.
    /// </summary>
    public IMultiplayerPlayer Player { get; }

    /// <summary>
    /// Gets the previous slot number.
    /// </summary>
    public int PreviousSlot { get; }

    /// <summary>
    /// Gets the new slot number.
    /// </summary>
    public int NewSlot { get; }
}