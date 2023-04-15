using BanchoSharp.Interfaces;

namespace BanchoSharp.EventArgs;

/// <summary>
/// Event arguments class for when a player is kicked from a multiplayer lobby.
/// </summary>
public class PlayerKickedEventArgs : System.EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerKickedEventArgs"/> class.
    /// </summary>
    /// <param name="player">The player who was kicked.</param>
    /// <param name="kickTime">The time at which the player was kicked.</param>
    public PlayerKickedEventArgs(IMultiplayerPlayer player, DateTime kickTime)
    {
        Player = player;
        KickTime = kickTime;
    }

    /// <summary>
    /// Gets the player who was kicked.
    /// </summary>
    public IMultiplayerPlayer Player { get; }

    /// <summary>
    /// Gets the time at which the player was kicked.
    /// </summary>
    public DateTime KickTime { get; }
}