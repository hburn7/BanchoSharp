using BanchoSharp.EventArgs;
using BanchoSharp.Multiplayer;
using System.ComponentModel;

namespace BanchoSharp.Interfaces;

public interface IMultiplayerLobby : IChatChannel, INotifyStateChanged
{
	/// <summary>
	/// The ID of the multiplayer lobby. This is the value at the end of the "mp link" (https://osu.ppy.sh/mp/ID)
	/// </summary>
	public long Id { get; }
	/// <summary>
	/// The name of the multiplayer lobby. e.g. "OWC 2021: (team A) vs. (team B)
	/// Not to be confused with FullName./>
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
	/// The number of players present in the lobby currently
	/// </summary>
	public int PlayerCount { get; }
	/// <summary>
	///  The current host of the lobby
	/// </summary>
	public MultiplayerPlayer? Host { get; }
	public bool HostIsChangingMap { get; }
	public bool MatchInProgress { get; }
	public bool IsLocked { get; }
	public bool IsClosed { get; }
	TimeSpan? LobbyTimerRemaining { get; }
	TimeSpan? MatchTimerRemaining { get; }
	public bool MatchStartTimerInProgress { get; }
	public bool LobbyTimerInProgress { get; }
	public LobbyFormat Format { get; }
	public WinCondition WinCondition { get; }
	public GameMode GameMode { get; }
	public List<MultiplayerPlayer> Players { get; }
	public List<string> Referees { get; }
	public Mods Mods { get; }
	public event Action OnSettingsUpdated;
	public event Action<int> OnLobbyTimerStarted;
	public event Action OnLobbyTimerFinished;
	public event Action<int> OnMatchStartTimerStarted;
	public event Action OnMatchStartTimerFinished;
	public event Action OnMatchAborted;
	public event Action OnMatchStarted;
	public event Action OnMatchFinished;
	public event Action OnClosed;
	public event Action OnAllPlayersReady;
	public event Action<MultiplayerPlayer> OnHostChanged;
	public event Action<BeatmapShell> OnBeatmapChanged;
	public event Action<MultiplayerPlayer> OnPlayerJoined;
	/// <summary>
	///  Invoked when the player changes their team color. The previous team color
	///  is provided.
	/// </summary>
	public event Action<PlayerChangedTeamEventArgs> OnPlayerChangedTeam;
	public event Action<PlayerSlotMoveEventArgs> OnPlayerSlotMove;
	public event Action<PlayerDisconnectedEventArgs> OnPlayerDisconnected;
	public event Action<PlayerKickedEventArgs> OnPlayerKicked;
	/// <summary>
	///  Invoked when the host is selecting a new map
	/// </summary>
	public event Action OnHostChangingMap;

	public Task UpdateSettingsAsync(LobbyFormat? format = LobbyFormat.TeamVs,
		WinCondition? winCondition = WinCondition.ScoreV2, GameMode? gameMode = GameMode.osu);

	public Task AbortAsync();
	public Task AbortTimerAsync();
	public Task RefreshSettingsAsync();
	public Task SetSizeAsync(int newSize);
	public Task MoveAsync(string player, int slot);
	public Task RenameAsync(string newName);
	public Task InviteAsync(string username);
	public Task LockAsync();
	public Task UnlockAsync();
	public Task CloseAsync();
	public Task ClearHostAsync();
	public Task SetHostAsync(string username);
	public Task SetModsAsync(params string[] mods);
	public Task SetModsAsync(string mods);
	public Task SetTimerAsync(int seconds);
	public Task SetMatchStartTimerAsync(int seconds);
	public Task StartAsync();
	public Task KickAsync(string username);
	public Task BanAsync(string username);
	public Task AddRefereeAsync(params string[] usernames);
	public Task RemoveRefereesAsync(params string[] usernames);
	public Task SetMapAsync(BeatmapShell beatmap);
	public Task SendHelpMessageAsync();
}