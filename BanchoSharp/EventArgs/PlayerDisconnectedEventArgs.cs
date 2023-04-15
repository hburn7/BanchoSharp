using BanchoSharp.Interfaces;

namespace BanchoSharp.EventArgs;

/// <summary>
/// Represents the event arguments for a player disconnection.
/// </summary>
public class PlayerDisconnectedEventArgs : System.EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerDisconnectedEventArgs"/> class.
    /// </summary>
    /// <param name="player">The <see cref="IMultiplayerPlayer"/> object related to the disconnection event.</param>
    /// <param name="disconnectedAt">The <see cref="DateTime"/> value of when the disconnection occurred.</param>
    public PlayerDisconnectedEventArgs(IMultiplayerPlayer player, DateTime disconnectedAt)
    {
        Player = player;
        DisconnectedAt = disconnectedAt;
    }

    /// <summary>
    /// Gets the <see cref="IMultiplayerPlayer"/> object related to the disconnection event.
    /// </summary>
    public IMultiplayerPlayer Player { get; }

    /// <summary>
    /// Gets the <see cref="DateTime"/> value of when the disconnection occurred.
    /// </summary>
    public DateTime DisconnectedAt { get; }
}
