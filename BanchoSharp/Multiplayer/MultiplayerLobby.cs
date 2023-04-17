using BanchoSharp.EventArgs;
using BanchoSharp.Interfaces;
using BanchoSharp.Messaging;
using BanchoSharp.Messaging.ChatMessages;
using System.Text.RegularExpressions;

namespace BanchoSharp.Multiplayer;

public enum LobbyFormat
{
	HeadToHead,
	TagCoop,
	TeamVs,
	TagTeamVs
}

public enum WinCondition
{
	Score,
	Accuracy,
	Combo,
	ScoreV2
}

public enum GameMode
{
	osu,
	osuMania,
	osuTaiko,
	osuCatch
}

public class BeatmapShell
{
	public BeatmapShell(int id, string? artist, string? title, string? difficulty,
		GameMode? gameMode)
	{
		Id = id;
		Artist = artist;
		Title = title;
		Difficulty = difficulty;
		GameMode = gameMode;
	}

	public int Id { get; }
	public string? Title { get; }
	public string? Artist { get; }
	public string? Difficulty { get; }
	public GameMode? GameMode { get; }

	public override bool Equals(object? obj)
	{
		if (obj is not BeatmapShell other)
		{
			return false;
		}

		return other.Id == Id && other.Title == Title && other.Artist == Artist && other.Difficulty == Difficulty && other.GameMode == GameMode;
	}

	protected bool Equals(BeatmapShell other) => Id == other.Id && Title == other.Title && Artist == other.Artist && Difficulty == other.Difficulty && GameMode == other.GameMode;
	public override int GetHashCode() => HashCode.Combine(Id, Title, Artist, Difficulty, GameMode);
}

/// <summary>
///  Note: This class is untested and is not officially supported yet.
/// </summary>
public sealed class MultiplayerLobby : Channel, IMultiplayerLobby
{
	private readonly IBanchoClient _client;
	private DateTime? _lobbyTimerEnd;
	private DateTime? _matchTimerEnd;
	private int _playersRemainingCount;

	public MultiplayerLobby(IBanchoClient client, long id, string name) : base($"#mp_{id}", client.ClientConfig.SaveMessags)
	{
		_client = client;

		Id = id;
		Name = name;
		Size = 16;
		GameMode = GameMode.osu;

		_client.OnMessageReceived += m =>
		{
			if (m is IPrivateIrcMessage { Sender: "BanchoBot" } pm && pm.Recipient == ChannelName)
			{
				UpdateLobbyFromBanchoBotSettingsResponse(pm.Content);
			}
		};

		OnMatchStarted += () =>
		{
			ResetLobbyTimer();
			ResetMatchTimer();
			InvokeOnStateChanged();
			SetAllPlayerStates(PlayerState.NotReady);
		};

		OnMatchFinished += () =>
		{
			ResetLobbyTimer();
			ResetMatchTimer();
			InvokeOnStateChanged();
		};

		OnPlayerJoined += player =>
		{
			Players.Add(player);
			InvokeOnStateChanged();
		};

		OnPlayerDisconnected += disconnectedEventArgs =>
		{
			Players.Remove(disconnectedEventArgs.Player);
			InvokeOnStateChanged();
		};

		OnPlayerKicked += kickedEventArgs =>
		{
			Players.Remove(kickedEventArgs.Player);
			InvokeOnStateChanged();
		};

		OnPlayerBanned += bannedEventArgs =>
		{
			Players.Remove(bannedEventArgs.Player);
			InvokeOnStateChanged();
		};

		OnBeatmapChanged += shell =>
		{
			CurrentBeatmap = shell;
			HostIsChangingMap = false;
		};

		OnFormatChanged += args =>
		{
			var oldFmt = args.PreviousLobbyFormat;
			var newFmt = args.CurrentLobbyFormat;
			
			if (newFmt is LobbyFormat.TagCoop or LobbyFormat.HeadToHead && oldFmt is LobbyFormat.TeamVs or LobbyFormat.TagTeamVs)
			{
				// If the format is changing from a team format to a non-team format, we need to remove all teams.
				// This is done by setting the team of all players to none.
				foreach (var player in Players)
				{
					player.Team = TeamColor.None;
				}
			}
			else
			{
				// Check for the opposite and warn
				if (oldFmt is LobbyFormat.TagCoop or LobbyFormat.HeadToHead && newFmt is LobbyFormat.TeamVs or LobbyFormat.TagTeamVs)
				{
					Logger.Warn("Lobby format was changed from a non-team format to a team format. " +
					            "There is no way to know what teams players are on without an !mp settings call.");
				}
			}
		};

		OnPlayerChangedTeam += _ => InvokeOnStateChanged();

		OnAllPlayersReady += () => SetAllPlayerStates(PlayerState.Ready);
	}

	public event Action? OnSettingsUpdated;
	public event Action<int>? OnLobbyTimerStarted;
	public event Action? OnLobbyTimerFinished;
	public event Action<int>? OnMatchStartTimerStarted;
	public event Action? OnMatchStartTimerFinished;
	public event Action? OnMatchAborted;
	public event Action? OnMatchStarted;
	public event Action? OnMatchFinished;
	public event Action<GameModeChangedEventArgs>? OnGameModeChanged;
	public event Action<FormatChangedEventArgs>? OnFormatChanged;
	public event Action<WinConditionChangedEventArgs>? OnWinConditionChanged;
	public event Action? OnClosed;
	public event Action<IMultiplayerPlayer>? OnHostChanged;
	public event Action<BeatmapShell>? OnBeatmapChanged;
	public event Action<IMultiplayerPlayer>? OnPlayerJoined;
	public event Action<PlayerChangedTeamEventArgs>? OnPlayerChangedTeam;
	public event Action<PlayerSlotMoveEventArgs>? OnPlayerSlotMove;
	public event Action<PlayerDisconnectedEventArgs>? OnPlayerDisconnected;
	public event Action<PlayerKickedEventArgs>? OnPlayerKicked;
	public event Action<PlayerBannedEventArgs>? OnPlayerBanned;
	public event Action? OnHostChangingMap;
	public long Id { get; }
	public string Name { get; private set; }
	public string HistoryUrl => $"https://osu.ppy.sh/mp/{Id}";
	public int Size { get; private set; } = 1;
	public int PlayerCount => Players.Count;
	public IMultiplayerPlayer? Host { get; private set; }
	public BeatmapShell? CurrentBeatmap { get; private set; }
	public bool HostIsChangingMap { get; private set; }
	public bool MatchInProgress { get; private set; }
	public bool IsLocked { get; private set; }
	public bool IsClosed { get; private set; }
	public TimeSpan? LobbyTimerRemaining => _lobbyTimerEnd?.Subtract(DateTime.Now);
	public TimeSpan? MatchTimerRemaining => _matchTimerEnd?.Subtract(DateTime.Now);
	public bool MatchStartTimerInProgress => MatchTimerRemaining > TimeSpan.FromSeconds(0);
	public bool LobbyTimerInProgress => LobbyTimerRemaining > TimeSpan.FromSeconds(0);
	public LobbyFormat Format { get; private set; } = LobbyFormat.HeadToHead;
	public WinCondition WinCondition { get; private set; } = WinCondition.Score;
	public GameMode GameMode { get; private set; } = GameMode.osu;
	public List<IMultiplayerPlayer> Players { get; } = new();
	public List<string> Referees { get; } = new();
	public Mods Mods { get; private set; } = Mods.None;

	public async Task UpdateSettingsAsync(LobbyFormat? format, WinCondition? winCondition, GameMode? gameMode)
	{
		format ??= Format;
		winCondition ??= WinCondition;
		gameMode ??= GameMode;

		Format = format.Value;
		WinCondition = winCondition.Value;
		GameMode = gameMode.Value;

		await SendAsync($"!mp set {(int)format} {(int)winCondition} {(int)gameMode}");
		InvokeOnStateChanged();
	}

	public async Task AbortAsync()
	{
		await SendAsync("!mp abort");
		MatchInProgress = false;

		InvokeOnStateChanged();
	}

	public async Task AbortTimerAsync()
	{
		await SendAsync("!mp aborttimer");
		ResetLobbyTimer();
		ResetMatchTimer();
		OnLobbyTimerFinished?.Invoke();
		InvokeOnStateChanged();
	}

	IMultiplayerPlayer? IMultiplayerLobby.FindPlayer(string username) => FindPlayer(username);
	public async Task RefreshSettingsAsync() => await SendAsync("!mp settings");

	public async Task SetSizeAsync(int newSize)
	{
		await SendAsync($"!mp size {newSize}");
		Size = newSize;
		InvokeOnStateChanged();
	}

	public async Task MoveAsync(IMultiplayerPlayer player, int slot) => await SendAsync($"!mp move {player} {slot}");

	public async Task RenameAsync(string newName)
	{
		await SendAsync($"!mp name {newName}");
		Name = newName;
		InvokeOnStateChanged();
	}

	public async Task InviteAsync(string username) => await SendAsync($"!mp invite {username}");

	public async Task LockAsync()
	{
		await SendAsync("!mp lock");
		IsLocked = true;
		InvokeOnStateChanged();
	}

	public async Task UnlockAsync()
	{
		await SendAsync("!mp unlock");
		IsLocked = false;
		InvokeOnStateChanged();
	}

	public async Task CloseAsync()
	{
		await SendAsync("!mp close");
		IsClosed = true;
		_client.Channels.Remove(this);
		OnClosed?.Invoke();
		InvokeOnStateChanged();
	}

	public async Task ClearHostAsync()
	{
		await SendAsync("!mp clearhost");
		Host = null;
		InvokeOnStateChanged();
	}

	public async Task SetHostAsync(IMultiplayerPlayer player)
	{
		await SendAsync($"!mp host {player.TargetableName()}");
		Host = player;
		OnHostChanged?.Invoke(player);
		InvokeOnStateChanged();
	}

	// todo: probably needs an associated event
	public async Task SetModsAsync(params string[] mods) => await SendAsync($"!mp mods {string.Join(" ", mods)}");
	public async Task SetModsAsync(string mods) => await SendAsync($"!mp mods {mods}");

	public async Task SetTimerAsync(int seconds)
	{
		await SendAsync($"!mp timer {seconds}");
		_lobbyTimerEnd = DateTime.Now.AddSeconds(seconds);
		OnLobbyTimerStarted?.Invoke(seconds);
		InvokeOnStateChanged();
	}

	public async Task SetMatchStartTimerAsync(int seconds)
	{
		await SendAsync($"!mp start {seconds}");
		_matchTimerEnd = DateTime.Now.AddSeconds(seconds);
		OnMatchStartTimerStarted?.Invoke(seconds);
		InvokeOnStateChanged();
	}

	public async Task StartAsync() => await SendAsync("!mp start");

	public async Task KickAsync(IMultiplayerPlayer player)
	{
		await SendAsync($"!mp kick {player.TargetableName()}");
		player.Lobby = null;

		OnPlayerKicked?.Invoke(new PlayerKickedEventArgs(player, DateTime.Now));
		InvokeOnStateChanged();
	}

	public async Task BanAsync(IMultiplayerPlayer player) => await SendAsync($"!mp ban {player.TargetableName()}");

	public async Task AddRefereeAsync(params string[] usernames)
	{
		await SendAsync($"!mp addref {string.Join(" ", usernames)}");
		Referees.AddRange(usernames);
		InvokeOnStateChanged();
	}

	public async Task RemoveRefereesAsync(params string[] usernames)
	{
		await SendAsync($"!mp removeref {string.Join(" ", usernames)}");
		Referees.RemoveAll(usernames.Contains);
		InvokeOnStateChanged();
	}

	public async Task SetMapAsync(BeatmapShell beatmap)
	{
		int modeNum;
		if (beatmap.GameMode.HasValue)
		{
			modeNum = (int)beatmap.GameMode.Value;
		}
		else
		{
			modeNum = (int)GameMode;
		}

		await SendAsync($"!mp map {beatmap.Id} {modeNum}");
	}

	public async Task SendHelpMessageAsync() => await SendAsync("!mp help");
	public event Action? OnAllPlayersReady;
	public event Action? OnStateChanged;
	public void InvokeOnStateChanged() => OnStateChanged?.Invoke();

	// todo: track player slots here
	public async Task MoveAsync(string player, int slot) => await SendAsync($"!mp move {player} {slot}");
	public async Task SetModsAsync(Mods mods) => await SendAsync($"!mp mods {mods}");
	private void ResetLobbyTimer() => _lobbyTimerEnd = null;
	private void ResetMatchTimer() => _matchTimerEnd = null;

	private void SetAllPlayerStates(PlayerState state)
	{
		foreach (var player in Players)
		{
			player.State = state;
		}
	}

	private void UpdateLobbyFromBanchoBotSettingsResponse(string banchoResponse)
	{
		var parser = new MpSetResponseParser(banchoResponse);
		var cfg = parser.ResolvedConfiguration;
		if (parser.IsMpSetResponse && cfg.HasValue)
		{
			if (Format != cfg.Value.Format)
			{
				OnFormatChanged?.Invoke(new FormatChangedEventArgs(Format, cfg.Value.Format));
				Format = cfg.Value.Format;
			}

			if (cfg.Value.WinCondition.HasValue && WinCondition != cfg.Value.WinCondition.Value)
			{
				OnWinConditionChanged?.Invoke(new WinConditionChangedEventArgs(WinCondition, cfg.Value.WinCondition.Value));
				WinCondition = cfg.Value.WinCondition!.Value;
			}

			if (cfg.Value.Size.HasValue)
			{
				Size = cfg.Value.Size.Value;
			}

			return;
		}

		if (IsRoomNameNotification(banchoResponse))
		{
			UpdateName(banchoResponse);
		}
		else if (IsGameModeUpdateNotification(banchoResponse))
		{
			UpdateGameMode(banchoResponse);
		}
		else if (IsSelfInvokedTeamSwapNotification(banchoResponse))
		{
			UpdatePlayerTeamSelfInvoked(banchoResponse);
		}
		else if (IsManualTeamSwapNotification(banchoResponse))
		{
			UpdatePlayerTeamManuallyInvoked(banchoResponse);
		}
		else if (IsTeamModeNotification(banchoResponse))
		{
			UpdateFormatWincondition(banchoResponse);
		}
		else if (IsHostChangingMapNotification(banchoResponse))
		{
			HostIsChangingMap = true;
			OnHostChangingMap?.Invoke();
		}
		else if (IsBeatmapChangedNotification(banchoResponse))
		{
			UpdateBeatmapChanged(banchoResponse);
		}
		else if (IsBeatmapSettingsNotification(banchoResponse))
		{
			UpdateBeatmapFromMpSettings(banchoResponse);
		}
		else if (IsBeatmapMpSetNotification(banchoResponse))
		{
			UpdateBeatmapFromMpSet(banchoResponse);
		}
		else if (IsPlayerJoinedInSlotNotification(banchoResponse))
		{
			UpdatePlayerJoinedInSlot(banchoResponse);
		}
		else if (IsMatchStartedNotification(banchoResponse))
		{
			OnMatchStarted?.Invoke();
			MatchInProgress = true;
		}
		else if (IsMpClearHostNotification(banchoResponse))
		{
			Host = null;
		}
		else if (IsMatchFinishedNotification(banchoResponse))
		{
			OnMatchFinished?.Invoke();
			MatchInProgress = false;
		}
		else if (IsPlayerMovedToSlotNotification(banchoResponse))
		{
			UpdatePlayerSlotMove(banchoResponse);
		}
		else if (IsPlayersNotification(banchoResponse))
		{
			UpdatePlayersRemaining(banchoResponse);
		}
		else if (IsSlotStatusNotification(banchoResponse))
		{
			UpdatePlayerInformation(banchoResponse);
		}
		else if (IsMatchHostChangedNotification(banchoResponse))
		{
			UpdateMatchHost(banchoResponse);
		}
		else if (IsMatchActiveModsNotification(banchoResponse))
		{
			UpdateMatchMods(banchoResponse);
		}
		else if (IsMatchModsUpdatedNotification(banchoResponse))
		{
			UpdateMatchMods(banchoResponse);
		}
		else if (IsPlayerFinishedNotification(banchoResponse))
		{
			UpdatePlayerResults(banchoResponse);
		}
		else if (IsPlayerLeftNotification(banchoResponse))
		{
			UpdatePlayerDisconnect(banchoResponse);
		}
		else if (IsAllPlayersReadyNotification(banchoResponse))
		{
			OnAllPlayersReady?.Invoke();
		}
		else if (IsMatchAbortedNotification(banchoResponse))
		{
			OnMatchAborted?.Invoke();
			MatchInProgress = false;
		}
		else if (IsPlayerBanNotification(banchoResponse))
		{
			string bannedPlayer = banchoResponse.Split()[1];
			var bannedPlayerObj = Players.FirstOrDefault(p => p.Name == bannedPlayer);
			if (bannedPlayerObj != null)
			{
				OnPlayerBanned?.Invoke(new PlayerBannedEventArgs(bannedPlayerObj, DateTime.Now));
			}
		}
		else if (IsMatchSizeNotification(banchoResponse))
		{
			if (!int.TryParse(banchoResponse.Split().Last(), out int size))
			{
				Logger.Warn($"Could not parse size from bancho bot match size notification: '{banchoResponse}'");
				return;
			}

			Size = size;
		}

		InvokeOnStateChanged();
	}

	private bool IsManualTeamSwapNotification(string banchoResponse) => banchoResponse.StartsWith("Moved") && banchoResponse.Contains("to team ");
	private bool IsRoomNameNotification(string banchoResponse) => banchoResponse.StartsWith("Room name:");
	private bool IsTeamModeNotification(string banchoResponse) => banchoResponse.StartsWith("Team mode:");
	private bool IsHostChangingMapNotification(string banchoResponse) => banchoResponse.Equals("Host is changing map...");
	private bool IsBeatmapSettingsNotification(string banchoResponse) => banchoResponse.StartsWith("Beatmap:");
	private bool IsBeatmapMpSetNotification(string banchoResponse) => banchoResponse.StartsWith("Changed beatmap to");
	private bool IsBeatmapChangedNotification(string banchoResponse) => banchoResponse.StartsWith("Beatmap changed to:");
	private bool IsMatchHostChangedNotification(string banchoResponse) => banchoResponse.EndsWith(" became the host.");
	private bool IsPlayerJoinedInSlotNotification(string banchoResponse) => banchoResponse.Contains(" joined in slot ");
	private bool IsMatchStartedNotification(string banchoResponse) => banchoResponse.Equals("The match has started!");
	private bool IsMpClearHostNotification(string banchoResponse) => banchoResponse.Equals("Cleared match host");
	private bool IsMatchFinishedNotification(string banchoResponse) => banchoResponse.Equals("The match has finished!");
	private bool IsPlayerMovedToSlotNotification(string banchoResponse) => banchoResponse.Contains("moved to slot");
	private bool IsPlayersNotification(string banchoResponse) => banchoResponse.StartsWith("Players:");

	// Example: "Slot 1  Not Ready https://osu.ppy.sh/u/00000000 Player      [Host / HardRock]"
	private bool IsSlotStatusNotification(string banchoResponse) => banchoResponse.StartsWith("Slot ");
	private bool IsMatchActiveModsNotification(string banchoResponse) => banchoResponse.StartsWith("Active mods: ");
	private bool IsPlayerBanNotification(string banchoResponse) => banchoResponse.StartsWith("Banned") && banchoResponse.Contains("from the match");
	private bool IsMatchModsUpdatedNotification(string banchoResponse) => banchoResponse.EndsWith("enabled FreeMod") || banchoResponse.EndsWith("disabled FreeMod");
	private bool IsPlayerFinishedNotification(string banchoResponse) => banchoResponse.Contains("finished playing (Score:");
	private bool IsPlayerLeftNotification(string banchoResponse) => banchoResponse.EndsWith(" left the game.");

	// todo: check if needed --> private bool IsPlayerKickedNotification(string banchoResponse) => banchoResponse.Contains("");
	private bool IsAllPlayersReadyNotification(string banchoResponse) => banchoResponse.StartsWith("All players are ready");
	private bool IsMatchAbortedNotification(string banchoResponse) => banchoResponse.StartsWith("Aborted the match");
	private bool IsMatchSizeNotification(string banchoResponse) => banchoResponse.StartsWith("Changed match to size");
	private bool IsGameModeUpdateNotification(string banchoResponse) => banchoResponse.StartsWith("Changed match mode to ");
	private bool IsSelfInvokedTeamSwapNotification(string banchoResponse) => banchoResponse.Contains("changed to Red") || banchoResponse.Contains("changed to Blue");

	private void UpdatePlayerTeamManuallyInvoked(string banchoResponse)
	{
		string[] splits = banchoResponse.Split();
		string player = splits[1];
		string team = splits[^1];

		if (team != "Red" && team != "Blue")
		{
			return;
		}

		var match = FindPlayer(player);
		if (match == null)
		{
			return;
		}

		var prevTeam = match.Team;
		switch (team.ToLower())
		{
			case "red":
				match.Team = TeamColor.Red;
				break;
			case "blue":
				match.Team = TeamColor.Blue;
				break;
		}

		OnPlayerChangedTeam?.Invoke(new PlayerChangedTeamEventArgs(match, prevTeam));
	}

	private void UpdateName(string banchoResponse)
	{
		// Process room name

		// Index of where the multiplayer lobby name begins
		int index = banchoResponse.LastIndexOf(',');
		string nameSub = banchoResponse[..index];
		string name = nameSub.Split("Room name:")[1].Trim();

		Name = name;
		InvokeOnStateChanged();
	}

	private void UpdatePlayerTeamSelfInvoked(string banchoResponse)
	{
		string player = banchoResponse.Split()[0];
		string team = banchoResponse.Split()[^1];

		if (team != "Red" && team != "Blue")
		{
			return;
		}

		var match = FindPlayer(player);
		if (match == null)
		{
			return;
		}

		var prevTeam = match.Team;
		switch (team.ToLower())
		{
			case "red":
				match.Team = TeamColor.Red;
				break;
			case "blue":
				match.Team = TeamColor.Blue;
				break;
		}

		OnPlayerChangedTeam?.Invoke(new PlayerChangedTeamEventArgs(match, prevTeam));
	}

	private void UpdateGameMode(string banchoResponse)
	{
		string[] splits = banchoResponse.Split("Changed match mode to ");
		string token = splits[1];
		GameMode? newGameMode = null;
		switch (token)
		{
			// Parse as GameMode enum
			case "Osu":
				newGameMode = GameMode.osu;
				break;
			case "Taiko":
				newGameMode = GameMode.osuTaiko;
				break;
			case "CatchTheBeat":
				newGameMode = GameMode.osuCatch;
				break;
			case "OsuMania":
				newGameMode = GameMode.osuMania;
				break;
			default:
				Logger.Warn($"Could not parse game mode from BanchoBot game mode update notification: '{banchoResponse}'");
				break;
		}

		if (newGameMode != null)
		{
			OnGameModeChanged?.Invoke(new GameModeChangedEventArgs(GameMode, newGameMode.Value));
			GameMode = newGameMode.Value;
		}
	}

	private IMultiplayerPlayer? FindPlayer(string name) => Players.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
	private IMultiplayerPlayer? FindPlayer(IMultiplayerPlayer player) => Players.FirstOrDefault(x => x.Equals(player));

	private WinCondition ParseWinCondition(string wc) => wc switch
	{
		"Score" => WinCondition.Score,
		"ScoreV2" => WinCondition.ScoreV2,
		"Accuracy" => WinCondition.Accuracy,
		"Combo" => WinCondition.Combo,
		_ => throw new InvalidOperationException($"Cannot parse win condition {wc}")
	};

	private LobbyFormat ParseFormat(string fmt) => fmt switch
	{
		"HeadToHead" => LobbyFormat.HeadToHead,
		"TeamVs" => LobbyFormat.TeamVs,
		"TagTeamVs" => LobbyFormat.TagTeamVs,
		"TagCoop" => LobbyFormat.TagCoop,
		_ => throw new InvalidOperationException($"Cannot parse format {fmt}")
	};

	private GameMode ParseGameMode(string gm) => gm switch
	{
		"osu" => GameMode.osu,
		"osuMania" => GameMode.osuMania,
		"osuTaiko" => GameMode.osuTaiko,
		"osuCatch" => GameMode.osuCatch,
		_ => throw new InvalidOperationException($"Cannot parse game mode {gm}")
	};

	private async Task SendAsync(string command)
	{
		if (_client.ClientConfig.SaveMessags)
		{
			_client.GetChannel(ChannelName)
			       ?.MessageHistory?.AddLast(
				       PrivateIrcMessage.CreateFromParameters(_client.ClientConfig.Credentials.Username, ChannelName, command));
		}

		await _client.SendAsync($"PRIVMSG {ChannelName} {command}");
	}

#region MultiplayerLobby update methods
	private void UpdateMatchHost(string banchoResponse)
	{
		string hostPlayerName = banchoResponse.Split(" became the host")[0];
		string? prevHostName = Host?.Name;

		Host = FindPlayer(hostPlayerName);

		if (Host is not null)
		{
			if (Host.Name != prevHostName)
			{
				OnHostChanged?.Invoke(Host);
			}
		}

		InvokeOnStateChanged();
	}

	private void UpdatePlayerInformation(string banchoResponse)
	{
		// Find the digit(s) within the first 8 characters, which is the slot number
		int slot = int.Parse(banchoResponse[..8].Where(char.IsDigit).ToArray());

		// Find the first ' ' after the URL, since the URL is not padded with any spaces.
		int playerNameBegin = banchoResponse.IndexOf(' ', banchoResponse.IndexOf("/u/", StringComparison.Ordinal)) + 1;

		string playerName = banchoResponse.Substring(playerNameBegin, 16).Trim();

		// Bancho may send extra player info after the name, for example "[Host / HardRock]", after the 16
		// character player name bit.
		string? playerInfo = banchoResponse.Length > (playerNameBegin + 16) ? banchoResponse[(playerNameBegin + 16)..] : null;

		int? playerId = null;

		// Attempt to find the digits from "/u/" to where the name begins, which is the player id.
		if (int.TryParse(banchoResponse[banchoResponse.IndexOf("/u/", StringComparison.Ordinal)..(playerNameBegin - 1)].Where(char.IsDigit).ToArray(), out int parsedPlayerId))
		{
			playerId = parsedPlayerId;
		}

		var player = FindPlayer(playerName);

		if (player is null)
		{
			player = new MultiplayerPlayer(this, playerName, slot)
			{
				Id = playerId
			};

			OnPlayerJoined?.Invoke(player);
		}
		else
		{
			if (player.Slot != slot)
			{
				int previousSlot = player.Slot;
				player.Slot = slot;

				OnPlayerSlotMove?.Invoke(new PlayerSlotMoveEventArgs(player, previousSlot, slot));
			}
		}

		PlayerState state;
		if (banchoResponse[..playerNameBegin].Contains("Not Ready"))
		{
			state = PlayerState.NotReady;
		}
		else if (banchoResponse[..playerNameBegin].Contains("Ready"))
		{
			state = PlayerState.Ready;
		}
		else if (banchoResponse[..playerNameBegin].Contains("No Map"))
		{
			state = PlayerState.NoMap;
		}
		else
		{
			state = PlayerState.Undefined;
		}

		player.State = state;
		player.Id = playerId;

		if (playerInfo != null)
		{
			// Just finding words in this string feels like an easier approach at the moment, since the string provided
			// by bancho seems to be using both '/' and ',' as a separator at the same time, and I don't see any
			// benefits with working that out right now.

			if (playerInfo.Contains("Host"))
			{
				string? prevHostName = Host?.Name;

				Host = player;

				if (Host.Name != prevHostName)
				{
					OnHostChanged?.Invoke(Host);
				}
			}

			if (playerInfo.Contains("Team "))
			{
				var prevTeam = player.Team;

				if (playerInfo.Contains("Team Blue") && player.Team != TeamColor.Blue)
				{
					player.Team = TeamColor.Blue;

					OnPlayerChangedTeam?.Invoke(new PlayerChangedTeamEventArgs(player, prevTeam));
				}

				if (playerInfo.Contains("Team Red") && player.Team != TeamColor.Red)
				{
					player.Team = TeamColor.Red;

					OnPlayerChangedTeam?.Invoke(new PlayerChangedTeamEventArgs(player, prevTeam));
				}
			}

			// Only attempt to find player mods if Freemod is enabled
			if ((Mods & Mods.Freemod) == Mods.Freemod)
			{
				player.Mods = Mods.None;

				// Since all mods should be correctly named directly within the enum, we should just
				// be able to match strings here.
				foreach (Mods mod in Enum.GetValues(typeof(Mods)))
				{
					if (mod == Mods.None)
					{
						continue;
					}

					string? modName = Enum.GetName(typeof(Mods), mod);

					if (modName == null)
					{
						continue;
					}

					// Bancho calls autopilot for "Relax2" for some reason
					if (modName == "Autopilot")
					{
						modName = "Relax2";
					}

					if (playerInfo.Contains(modName))
					{
						player.Mods |= mod;
					}
				}
			}
			else
			{
				// Otherwise just apply the room mods to the player
				player.Mods = Mods;
			}
		}

		// Subtract the players remaining counter for each "slot" message we receive,
		// so we can invoke OnSettingsUpdated() once the counter reaches 0, 
		// which is the last message bancho will send us after "!mp settings"
		_playersRemainingCount--;

		if (_playersRemainingCount == 0)
		{
			OnSettingsUpdated?.Invoke();
		}

		InvokeOnStateChanged();
	}

	private void UpdatePlayersRemaining(string banchoResponse)
	{
		int playerCount = int.Parse(banchoResponse.Split("Players: ")[1]);

		if (playerCount == 0)
		{
			// The "Players: <count>" should only come after a !mp settings request, so if we've gotten this, 
			// and the count is 0, the "!mp settings" should have finished.

			OnSettingsUpdated?.Invoke();
		}
		else
		{
			_playersRemainingCount = playerCount;
		}

		InvokeOnStateChanged();
	}

	private void UpdatePlayerSlotMove(string banchoResponse)
	{
		string[] splits = banchoResponse.Split(" moved to slot ");
		string name = splits[0].Trim();
		string slot = splits[1].Trim();
		int slotNum = int.Parse(slot);

		var player = FindPlayer(name);

		if (player is null)
		{
			player = new MultiplayerPlayer(this, name, slotNum);
			Players.Add(player);
		}

		int previousSlot = player!.Slot;
		player.Slot = slotNum;

		OnPlayerSlotMove?.Invoke(new PlayerSlotMoveEventArgs(player, previousSlot, slotNum));
		InvokeOnStateChanged();
	}

	// Handles joining in a slot *and* joining in a team slot.
	private void UpdatePlayerJoinedInSlot(string banchoResponse)
	{
		string[] splits = banchoResponse.Split(" joined in slot ");
		string playerName = splits[0];

		if (banchoResponse.Contains("for team"))
		{
			int slotNum = int.Parse(splits[1][..2].Trim()); // Ignore trailing period
			string team = banchoResponse.Split(" for team ")[1][..^1];
			var color = team == "blue" ? TeamColor.Blue : TeamColor.Red;
			OnPlayerJoined?.Invoke(new MultiplayerPlayer(this, playerName, slotNum, color));
		}
		else
		{
			int slotNum = int.Parse(splits[1][..^1]); // Ignore trailing period
			OnPlayerJoined?.Invoke(new MultiplayerPlayer(this, playerName, slotNum));
		}

		InvokeOnStateChanged();
	}

	/// <summary>
	///  Called when !mp settings displays the current beatmap.
	/// </summary>
	/// <param name="banchoResponse"></param>
	private void UpdateBeatmapFromMpSettings(string banchoResponse)
	{
		int id = -1;
		string difficulty, artist, title;
		difficulty = artist = title = null;

		string[] splits = banchoResponse.Split(" - ");

		try
		{
			id = int.Parse(splits[0].Split()[1].Split('/').Last());
			artist = splits[0].Split().Last();
			title = splits[1].Split('[').First().Trim();
			difficulty = splits[1].Split('[')[1].Split(']')[0];
		}
		catch (IndexOutOfRangeException)
		{
			//
		}

		OnBeatmapChanged?.Invoke(new BeatmapShell(id, artist, title, difficulty, GameMode));
		InvokeOnStateChanged();
	}

	/// <summary>
	///  Called when !mp set is used to change the beatmap.
	/// </summary>
	/// <param name="banchoResponse"></param>
	private void UpdateBeatmapFromMpSet(string banchoResponse)
	{
		// Changed beatmap to https://osu.ppy.sh/b/35165 dBu Music - Border of Life
		// Changed beatmap to https://osu.ppy.sh/b/28493 Hitomi Sato, Junichi Masuda - Battle! Gym Leader
		// Only happens via !mp set

		string artist = "";
		string title = "";

		string[] splits = banchoResponse.Split(" - ");
		if (!int.TryParse(splits[0].Split("https://osu.ppy.sh/b/")[1].Split()[0], out int id))
		{
			Logger.Warn("Could not determine beatmap ID from bancho response: " + banchoResponse);
		}

		try
		{
			artist = splits[1];
			title = splits[0].Split(id.ToString())[1].Split(" - ")[0].Trim();
		}
		catch (Exception e)
		{
			Logger.Warn($"Failed to parse beatmap info from bancho response: {banchoResponse}. Exception: {e.Message}");
			if (e.StackTrace != null)
			{
				Logger.Debug(e.StackTrace);
			}
		}
		finally
		{
			// There seems to not be difficulty information from !mp set
			OnBeatmapChanged?.Invoke(new BeatmapShell(id, artist, title, "<<unknown>>", GameMode));
		}
	}

	private void UpdateBeatmapChanged(string banchoResponse)
	{
		// Beatmap changed to: Camellia - Feelin Sky (Camellia\'s "200step" Self-remix) [Ambivalence] (https://osu.ppy.sh/b/1314987)
		// Beatmap changed to: Blue Stahli - Anti You [[[REMAP]]] (https://osu.ppy.sh/b/146540)

		string[] splits = banchoResponse.Split("Beatmap changed to: ");

		if (!int.TryParse(splits[1].Split(" (https://osu.ppy.sh/b/")[1].Split(')')[0], out int id))
		{
			Logger.Warn("Could not determine beatmap ID from bancho response: " + banchoResponse);
		}

		var difficultyRegex = new Regex(@"\[.+\]");

		string artist = "<unknown>";
		string title = "<unknown>";
		string difficulty = "<unknown>";

		try
		{
			if (banchoResponse.Contains('[') && banchoResponse.Contains(']'))
			{
				artist = splits[1].Split('-')[0].Trim();
				title = splits[1].Split('-')[1].Split('[')[0].Trim();

				if (difficultyRegex.IsMatch(banchoResponse))
				{
					// Remove leading and trailing brackets
					difficulty = difficultyRegex.Match(banchoResponse).Value[1..^1];
				}
			}
		}
		catch (Exception e)
		{
			Logger.Warn($"Failed to parse beatmap info from bancho response: {banchoResponse}. Exception: {e.Message}");
			if (e.StackTrace != null)
			{
				Logger.Debug(e.StackTrace);
			}
		}
		finally
		{
			OnBeatmapChanged?.Invoke(new BeatmapShell(id, artist, title, difficulty, GameMode));
			InvokeOnStateChanged();
		}
	}

	private void UpdateFormatWincondition(string banchoResponse)
	{
		// Update team mode and win condition

		// Index of where the team mode string begins
		int index = banchoResponse.LastIndexOf(',');

		string winConditionSub = banchoResponse[(index + 1)..];
		string winCondition = winConditionSub.Split(':')[1].Trim();

		string formatSub = banchoResponse[..index];
		string format = formatSub.Split(':')[1].Trim();

		var newFormat = ParseFormat(format);
		var newWinCondition = ParseWinCondition(winCondition);

		if (Format != newFormat)
		{
			OnFormatChanged?.Invoke(new FormatChangedEventArgs(Format, newFormat));
			Format = newFormat;
		}

		if (WinCondition != newWinCondition)
		{
			OnWinConditionChanged?.Invoke(new WinConditionChangedEventArgs(WinCondition, newWinCondition));
			WinCondition = newWinCondition;
		}

		InvokeOnStateChanged();
	}

	private void UpdateMatchMods(string banchoResponse)
	{
		string modList;

		if (IsMatchModsUpdatedNotification(banchoResponse))
		{
			// This notification comes after running "!mp mods <mods>"

			if (banchoResponse.Equals("Disabled all mods, disabled FreeMod"))
			{
				Mods = Mods.None;
				return;
			}

			if (banchoResponse.Equals("Disabled all mods, enabled FreeMod"))
			{
				Mods = Mods.Freemod;
				return;
			}

			// Get the mods part from the response, example: "Enabled Hidden, HardRock, disabled Freemod"
			modList = banchoResponse.Split(", disabled FreeMod")[0].Trim()[8..];
		}
		else
		{
			// Otherwise it came after running "!mp settings"
			modList = banchoResponse.Split("Active mods: ")[1].Trim();
		}

		var mods = modList.Split(',').ToList();

		// If only a single mod was selected, there is no ',' separator in the string
		// so just add it manually
		if (!mods.Any())
		{
			mods.Add(modList);
		}

		Mods = Mods.None;

		foreach (string modStr in mods)
		{
			if (Enum.TryParse(modStr, out Mods mod))
			{
				Mods |= mod;
			}
			else
			{
				// Bancho calls autopilot for "Relax2" for some reason.
				if (modStr == "Relax2")
				{
					Mods |= Mods.Autopilot;
					continue;
				}

				Logger.Warn($"Failed to parse mod called: {modStr}");
			}
		}

		// Bancho will report that both Doubletime and Nightcore is enabled whenever Nightcore is picked, causing
		// the parsing above to mark both mods as enabled. So if nightcore is set, disable doubletime.
		if ((Mods & Mods.Nightcore) != 0)
			Mods &= ~Mods.DoubleTime;

		// Update mods for all players in the lobby
		foreach (var player in Players)
		{
			player.Mods = Mods;
		}

		InvokeOnStateChanged();
	}

	private void UpdatePlayerResults(string banchoResponse)
	{
		// Example input: "Player 1 finished playing (Score: 3818280, PASSED)."

		// Grab the player name from the beginning of the string
		string playerName = banchoResponse[..banchoResponse.IndexOf(" finished playing (Score: ", StringComparison.Ordinal)];
		var player = FindPlayer(playerName);

		if (player is null)
		{
			return;
		}

		// Grab everything after the "(Score: " part, so for example "3818280, PASSED)."
		string resultStr = banchoResponse[(banchoResponse.IndexOf(" finished playing (Score: ", StringComparison.Ordinal) + 26)..];

		int score = int.Parse(resultStr.Split(',')[0]);

		player.Score = score;
		player.Passed = resultStr.Contains("PASSED");
		InvokeOnStateChanged();
	}

	private void UpdatePlayerDisconnect(string banchoResponse)
	{
		string playerName = banchoResponse.Split(" left the game")[0];

		var player = FindPlayer(playerName);

		if (player is null)
		{
			return;
		}

		OnPlayerDisconnected?.Invoke(new PlayerDisconnectedEventArgs(player, DateTime.Now));
		InvokeOnStateChanged();
	}
#endregion
}