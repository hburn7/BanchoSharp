using BanchoSharp.Multiplayer;

namespace BanchoSharp.Interfaces;

/// <summary>
/// Interface for a player in a multiplayer lobby
/// </summary>
public interface IMultiplayerPlayer
{
    /// <summary>
    /// Gets or sets the unique identifier of the player
    /// </summary>
    public int? Id { get; set; }

    /// <summary>
    /// Gets the name of the player
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the team color of the player
    /// </summary>
    public TeamColor Team { get; set; }

    /// <summary>
    /// Gets or sets the slot number of the player
    /// </summary>
    public int Slot { get; set; }

    /// <summary>
    /// Gets or sets the mods used by the player
    /// </summary>
    public Mods Mods { get; set; }

    /// <summary>
    /// The score history of this player for the current lobby
    /// </summary>
    public List<IMultiplayerScoreReport> ScoreHistory { get; }

    /// <summary>
    /// Gets or sets a value indicating if the player is ready
    /// </summary>
    [Obsolete("IsReady is deprecated, use PlayerState instead", true)]
    public bool IsReady { get; set; }

    /// <summary>
    /// Gets or sets the state of the player
    /// </summary>
    public PlayerState State { get; set; }

    /// <summary>
    /// Gets or sets the lobby the player is in
    /// </summary>
    public IMultiplayerLobby? Lobby { get; set; }

    /// <summary>
    /// Returns the name of the player or the Id preceded by a "#" if Id is not null, for use in targeting the player in chat
    /// </summary>
    /// <returns>The targetable name of the player</returns>
    public string TargetableName();
    
    public void AddScoreReport(IMultiplayerScoreReport report);
    public bool RemoveScoreReport(IMultiplayerScoreReport report);

    public bool Equals(object? other);
    public bool Equals(MultiplayerPlayer other);
    public int GetHashCode();
}