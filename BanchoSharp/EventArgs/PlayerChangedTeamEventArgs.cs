using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;

namespace BanchoSharp.EventArgs;

/// <summary>
/// EventArgs for when a player changes teams in a multiplayer lobby.
/// </summary>
public class PlayerChangedTeamEventArgs : System.EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerChangedTeamEventArgs"/> class.
    /// </summary>
    /// <param name="player">The player who changed teams.</param>
    /// <param name="previousTeam">The team the player was previously on.</param>
    public PlayerChangedTeamEventArgs(IMultiplayerPlayer player, TeamColor previousTeam)
    {
        Player = player;
        PreviousTeam = previousTeam;
    }

    /// <summary>
    /// Gets the player who changed teams.
    /// </summary>
    public IMultiplayerPlayer Player { get; }

    /// <summary>
    /// Gets the team the player was previously on.
    /// </summary>
    public TeamColor PreviousTeam { get; }
}