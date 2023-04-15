using BanchoSharp.EventArgs;
using BanchoSharp.Multiplayer;

namespace BanchoSharp.Interfaces;

public interface IMultiplayerLobby : IChatChannel, INotifyStateChanged
{
    /// <summary>
    ///  The ID of the multiplayer lobby. This is the value at the end of the "mp link" (https://osu.ppy.sh/mp/ID)
    /// </summary>
    public long Id { get; }
    /// <summary>
    ///  The name of the multiplayer lobby. e.g. "OWC 2021: (team A) vs. (team B)" - Not to be confused with FullName
    /// </summary>
    public string Name { get; }
    /// <summary>
    ///  The link to the match history
    /// </summary>
    public string HistoryUrl { get; }
    /// <summary>
    ///  The current lobby capacity
    /// </summary>
    public int Size { get; }
    /// <summary>
    ///  The number of players present in the lobby currently
    /// </summary>
    public int PlayerCount { get; }
    /// <summary>
    ///  The current host of the lobby
    /// </summary>
    public IMultiplayerPlayer? Host { get; }

    /// <summary>
    /// The currently selected beatmap in the lobby
    /// </summary>
    public BeatmapShell? CurrentBeatmap { get; }

    /// <summary>
    /// A flag indicating if the host is in the process of changing the beatmap
    /// </summary>
    public bool HostIsChangingMap { get; }

    /// <summary>
    /// A flag indicating if a match is currently in progress
    /// </summary>
    public bool MatchInProgress { get; }

    /// <summary>
    /// A flag indicating if the lobby is locked, preventing players from changing slots
    /// </summary>
    public bool IsLocked { get; }

    /// <summary>
    /// A flag indicating if the lobby is completely closed and unavailable
    /// </summary>
    public bool IsClosed { get; }

    /// <summary>
    /// The remaining time for the lobby timer to expire
    /// </summary>
    public TimeSpan? LobbyTimerRemaining { get; }

    /// <summary>
    /// The remaining time for the match timer to expire
    /// </summary>
    public TimeSpan? MatchTimerRemaining { get; }

    /// <summary>
    /// A flag indicating if the match start timer is in progress
    /// </summary>
    public bool MatchStartTimerInProgress { get; }

    /// <summary>
    /// A flag indicating if the lobby timer is in progress
    /// </summary>
    public bool LobbyTimerInProgress { get; }

    /// <summary>
    /// The format of the multiplayer lobby
    /// </summary>
    public LobbyFormat Format { get; }

    /// <summary>
    /// The win condition for the multiplayer lobby
    /// </summary>
    public WinCondition WinCondition { get; }

    /// <summary>
    /// The game mode for the multiplayer lobby
    /// </summary>
    public GameMode GameMode { get; }

    /// <summary>
    /// The list of players currently in the multiplayer lobby
    /// </summary>
    public List<IMultiplayerPlayer> Players { get; }
    /// <summary>
    /// List of referees for the multiplayer match.
    /// </summary>
    public List<string> Referees { get; }

    /// <summary>
    /// Mods selected for the multiplayer match.
    /// </summary>
    public Mods Mods { get; }

    /// <summary>
    /// Event triggered when match settings have been updated.
    /// </summary>
    public event Action OnSettingsUpdated;

    /// <summary>
    /// Event triggered when the lobby timer has started.
    /// </summary>
    public event Action<int> OnLobbyTimerStarted;

    /// <summary>
    /// Event triggered when the lobby timer has finished.
    /// </summary>
    public event Action OnLobbyTimerFinished;

    /// <summary>
    /// Event triggered when the match start timer has started.
    /// </summary>
    public event Action<int> OnMatchStartTimerStarted;

    /// <summary>
    /// Event triggered when the match start timer has finished.
    /// </summary>
    public event Action OnMatchStartTimerFinished;

    /// <summary>
    /// Event triggered when the match has been aborted.
    /// </summary>
    public event Action OnMatchAborted;

    /// <summary>
    /// Event triggered when the match has started.
    /// </summary>
    public event Action OnMatchStarted;

    /// <summary>
    /// Event triggered when the match has finished.
    /// </summary>
    public event Action OnMatchFinished;

    /// <summary>
    /// Event triggered when the game mode has changed.
    /// </summary>
    public event Action<GameModeChangedEventArgs> OnGameModeChanged;

    /// <summary>
    /// Event triggered when the lobby format has changed.
    /// </summary>
    public event Action<FormatChangedEventArgs> OnFormatChanged;

    /// <summary>
    /// Event triggered when the win condition has changed.
    /// </summary>
    public event Action<WinConditionChangedEventArgs> OnWinConditionChanged;

    /// <summary>
    /// Event triggered when the multiplayer lobby has been closed.
    /// </summary>
    public event Action OnClosed;

    /// <summary>
    /// Event triggered when all players are ready.
    /// </summary>
    public event Action OnAllPlayersReady;

    /// <summary>
    /// Event triggered when the host player has changed.
    /// </summary>
    /// <param name="host">New host player.</param>
    public event Action<IMultiplayerPlayer> OnHostChanged;

    /// <summary>
    /// Event triggered when the beatmap has been changed.
    /// </summary>
    /// <param name="beatmap">Newly selected beatmap.</param>
    public event Action<BeatmapShell> OnBeatmapChanged;

    /// <summary>
    /// Event triggered when a new player has joined the multiplayer match.
    /// </summary>
    /// <param name="player">The new player object.</param>
    public event Action<IMultiplayerPlayer> OnPlayerJoined;

    /// <summary>
    ///  Invoked when the player changes their team color. The previous team color
    ///  is provided.
    /// </summary>
    public event Action<PlayerChangedTeamEventArgs> OnPlayerChangedTeam;
    /// <summary>
    /// Event that is triggered when a player changes their slot within the lobby.
    /// </summary>
    public event Action<PlayerSlotMoveEventArgs> OnPlayerSlotMove;

    /// <summary>
    /// Event that is triggered when a player disconnects from the lobby or match.
    /// </summary>
    public event Action<PlayerDisconnectedEventArgs> OnPlayerDisconnected;

    /// <summary>
    /// Event that is triggered when a player is kicked from the lobby or match.
    /// </summary>
    public event Action<PlayerKickedEventArgs> OnPlayerKicked;

    /// <summary>
    /// Event that is triggered when a player is banned from the lobby or match.
    /// </summary>
    public event Action<PlayerBannedEventArgs> OnPlayerBanned;

    /// <summary>
    ///  Invoked when the host is selecting a new map
    /// </summary>
    public event Action OnHostChangingMap;

    /// <summary>
    /// Updates the settings of the multiplayer lobby.
    /// </summary>
    /// <param name="format">The format of the lobby.</param>
    /// <param name="winCondition">The win condition of the lobby.</param>
    /// <param name="gameMode">The game mode of the lobby.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task UpdateSettingsAsync(LobbyFormat? format = LobbyFormat.TeamVs,
        WinCondition? winCondition = WinCondition.ScoreV2, GameMode? gameMode = GameMode.osu);

    /// <summary>
    /// Aborts an asynchronous operation for the multiplayer lobby.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AbortAsync();

    /// <summary>
    /// Aborts a timer's asynchronous operation for the multiplayer lobby.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AbortTimerAsync();

    /// <summary>
    /// Finds a player by their username.
    /// </summary>
    /// <param name="username">The username of the player.</param>
    /// <returns>The multiplayer player found or null if not found.</returns>
    public IMultiplayerPlayer? FindPlayer(string username);

    /// <summary>
    /// Refreshes the settings of the multiplayer lobby asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RefreshSettingsAsync();

    /// <summary>
    /// Sets the size of the multiplayer lobby.
    /// </summary>
    /// <param name="newSize">The new size of the lobby.</param>
    public Task SetSizeAsync(int newSize);

    /// <summary>
    /// Moves a player's slot in the multiplayer lobby.
    /// </summary>
    /// <param name="player">The player to move.</param>
    /// <param name="slot">The new slot number for the player.</param>
    public Task MoveAsync(IMultiplayerPlayer player, int slot);

    /// <summary>
    /// Renames the current player in the multiplayer lobby.
    /// </summary>
    /// <param name="newName">The new username to use.</param>
    public Task RenameAsync(string newName);

    /// <summary>
    /// Invites another player to the multiplayer lobby.
    /// </summary>
    /// <param name="username">The username of the player to invite.</param>
    public Task InviteAsync(string username);

    /// <summary>
    /// Locks the multiplayer lobby, preventing players from changing slots.
    /// </summary>
    public Task LockAsync();

    /// <summary>
    /// Unlocks the multiplayer lobby so players can change slots.
    /// </summary>
    public Task UnlockAsync();

    /// <summary>
    /// Closes the multiplayer lobby.
    /// </summary>
    public Task CloseAsync();

    /// <summary>
    /// Clears the host of the multiplayer lobby.
    /// </summary>
    public Task ClearHostAsync();

    /// <summary>
    /// Sets the host of the multiplayer lobby.
    /// </summary>
    /// <param name="player">The player to make the new host.</param>
    public Task SetHostAsync(IMultiplayerPlayer player);

    /// <summary>
    /// Sets the mods for the multiplayer lobby.
    /// </summary>
    /// <param name="mods">The string representation of the mods to use.</param>
    public Task SetModsAsync(params string[] mods);

    /// <summary>
    /// Sets the mods for the multiplayer lobby.
    /// </summary>
    /// <param name="mods">The string representation of the mods to use.</param>
    public Task SetModsAsync(string mods);

    /// <summary>
    /// Starts a timer in the multiplayer lobby for the given duration.
    /// </summary>
    /// <param name="seconds">The number of seconds to set the timer to.</param>
    public Task SetTimerAsync(int seconds);
    /// <summary>
    /// Starts the match after the provided time in seconds.
    /// </summary>
    /// <param name="seconds">The number of seconds to set the timer to.</param>
    /// <returns>A task that completes when the timer is set.</returns>
    public Task SetMatchStartTimerAsync(int seconds);

    /// <summary>
    /// Starts the match.
    /// </summary>
    /// <returns>A task that completes when the match is started.</returns>
    public Task StartAsync();

    /// <summary>
    /// Kicks a player from the lobby.
    /// </summary>
    /// <param name="player">The player to kick.</param>
    /// <returns>A task that completes after the player has been kicked.</returns>
    public Task KickAsync(IMultiplayerPlayer player);

    /// <summary>
    /// Bans a player from the lobby.
    /// </summary>
    /// <param name="player">The player to ban.</param>
    /// <returns>A task that completes after the player has been banned.</returns>
    public Task BanAsync(IMultiplayerPlayer player);

    /// <summary>
    /// Adds the specified user(s) to the list of referees for the current game.
    /// </summary>
    /// <param name="usernames">The usernames to add as referees.</param>
    /// <returns>A task that completes after the specified referees have been added.</returns>
    public Task AddRefereeAsync(params string[] usernames);

    /// <summary>
    /// Removes the specified user(s) from the list of referees for the current game.
    /// </summary>
    /// <param name="usernames">The usernames to remove from the list of referees.</param>
    /// <returns>A task that completes after the specified referees have been removed.</returns>
    public Task RemoveRefereesAsync(params string[] usernames);

    /// <summary>
    /// Sets the map for the lobby.
    /// </summary>
    /// <param name="beatmap">The new beatmap to use.</param>
    /// <returns>A task that completes after the map has been set.</returns>
    public Task SetMapAsync(BeatmapShell beatmap);

    /// <summary>
    /// Sends a help message (!mp help).
    /// </summary>
    /// <returns>A task that completes after the message has been sent.</returns>
    public Task SendHelpMessageAsync();
}