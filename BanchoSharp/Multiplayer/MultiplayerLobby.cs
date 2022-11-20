using BanchoSharp.EventArgs;
using BanchoSharp.Interfaces;
using BanchoSharp.Messaging;

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
	ScoreV2,
	Accuracy,
	Combo
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
	public BeatmapShell(int id, GameMode? gameMode)
	{
		Id = id;
		GameMode = gameMode;
	}

	public int Id { get; }
	public GameMode? GameMode { get; }
}

/// <summary>
///  Note: This class is untested and is not officially supported yet.
/// </summary>
public class MultiplayerLobby : Channel, IMultiplayerLobby
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
		};

		OnMatchFinished += () =>
		{
			ResetLobbyTimer();
			ResetMatchTimer();
		};

		OnPlayerJoined += player => Players.Add(player);
		OnPlayerDisconnected += disconnectedEventArgs => Players.Remove(disconnectedEventArgs.Player);
	}

	public event Action? OnSettingsUpdated;
	public event Action<int>? OnLobbyTimerStarted;
	public event Action? OnLobbyTimerFinished;
	public event Action<int>? OnMatchStartTimerStarted;
	public event Action? OnMatchStartTimerFinished;
	public event Action? OnMatchAborted;
	public event Action? OnMatchStarted;
	public event Action? OnMatchFinished;
	public event Action? OnClosed;
	public event Action<MultiplayerPlayer>? OnHostChanged;
	public event Action<BeatmapShell>? OnBeatmapChanged;
	public event Action<MultiplayerPlayer>? OnPlayerJoined;
	public event Action<PlayerChangedTeamEventArgs>? OnPlayerChangedTeam;
	public event Action<PlayerSlotMoveEventArgs>? OnPlayerSlotMove;
	public event Action<PlayerDisconnectedEventArgs>? OnPlayerDisconnected;
	public event Action? OnHostChangingMap;
	public long Id { get; }
	public string Name { get; private set; }
	public string HistoryUrl => $"https://osu.ppy.sh/mp/{Id}";
	public int Size { get; private set; } = 1;
	public MultiplayerPlayer? Host { get; private set; }
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
	public List<MultiplayerPlayer> Players { get; } = new();
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
	}

	public async Task AbortAsync()
	{
		await SendAsync("!mp abort");
		MatchInProgress = false;
	}

	public async Task AbortTimerAsync()
	{
		await SendAsync("!mp aborttimer");
		ResetLobbyTimer();
		ResetMatchTimer();
		OnLobbyTimerFinished?.Invoke();
	}

	public async Task DisplaySettingsAsync() => await SendAsync("!mp settings");

	public async Task SetSizeAsync(int newSize)
	{
		await SendAsync($"!mp size {newSize}");
		Size = newSize;
	}

	public async Task MoveAsync(string player, int slot) => await SendAsync($"!mp move {player} {slot}");

	public async Task RenameAsync(string newName)
	{
		await SendAsync($"!mp name {newName}");
		Name = newName;
	}

	public async Task InviteAsync(string username) => await SendAsync($"!mp invite {username}");

	public async Task LockAsync()
	{
		await SendAsync("!mp lock");
		IsLocked = true;
	}

	public async Task UnlockAsync()
	{
		await SendAsync("!mp unlock");
		IsLocked = false;
	}

	public async Task CloseAsync()
	{
		await SendAsync("!mp close");
		IsClosed = true;
		_client.Channels.Remove(this);
		OnClosed?.Invoke();
	}

	public async Task ClearHostAsync()
	{
		await SendAsync("!mp clearhost");
		Host = null;
	}

	public async Task SetHostAsync(string username)
	{
		await SendAsync($"!mp host {username}");
		Host = FindPlayer(username);

		OnHostChanged?.Invoke(Host!);
	}

	public async Task SetModsAsync(params string[] mods) => await SendAsync($"!mp mods {string.Join(" ", mods)}");
	public async Task SetModsAsync(string mods) => await SendAsync($"!mp mods {mods}");

	public async Task SetTimerAsync(int seconds)
	{
		await SendAsync($"!mp timer {seconds}");
		_lobbyTimerEnd = DateTime.Now.AddSeconds(seconds);
		OnLobbyTimerStarted?.Invoke(seconds);
	}

	public async Task SetMatchStartTimerAsync(int seconds)
	{
		await SendAsync($"!mp start {seconds}");
		_matchTimerEnd = DateTime.Now.AddSeconds(seconds);
		OnMatchStartTimerStarted?.Invoke(seconds);
	}

	public async Task StartAsync() => await SendAsync("!mp start");
	public async Task KickAsync(string username) => await SendAsync($"!mp kick {username}");
	public async Task BanAsync(string username) => await SendAsync($"!mp ban {username}");

	public async Task AddRefereeAsync(params string[] usernames)
	{
		await SendAsync($"!mp addref {string.Join(" ", usernames)}");
		Referees.AddRange(usernames);
	}

	public async Task RemoveRefereesAsync(params string[] usernames)
	{
		await SendAsync($"!mp removeref {string.Join(" ", usernames)}");
		Referees.RemoveAll(usernames.Contains);
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
	private void ResetLobbyTimer() => _lobbyTimerEnd = null;
	private void ResetMatchTimer() => _matchTimerEnd = null;

	private void UpdateLobbyFromBanchoBotSettingsResponse(string banchoResponse)
	{
		if (IsRoomNameNotification(banchoResponse))
		{
			UpdateName(banchoResponse);
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
			UpdateHostChangedBeatmap(banchoResponse);
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
		}
	}

	private bool IsRoomNameNotification(string banchoResponse) => banchoResponse.StartsWith("Room name: ");
	private bool IsTeamModeNotification(string banchoResponse) => banchoResponse.StartsWith("Team mode: ");
	private bool IsHostChangingMapNotification(string banchoResponse) => banchoResponse.Equals("Host is changing map...");
	private bool IsBeatmapChangedNotification(string banchoResponse) => banchoResponse.StartsWith("Beatmap changed to: ");
	private bool IsMatchHostChangedNotification(string banchoResponse) => banchoResponse.StartsWith("Changed match host to ");
	private bool IsPlayerJoinedInSlotNotification(string banchoResponse) => banchoResponse.Contains(" joined in slot ");
	private bool IsMatchStartedNotification(string banchoResponse) => banchoResponse.Equals("The match has started!");
	private bool IsMatchFinishedNotification(string banchoResponse) => banchoResponse.Equals("The match has finished!");
	private bool IsPlayerMovedToSlotNotification(string banchoResponse) => banchoResponse.Contains("moved to slot");
	private bool IsPlayersNotification(string banchoResponse) => banchoResponse.StartsWith("Players: ");

	// Example: "Slot 1  Not Ready https://osu.ppy.sh/u/00000000 Player      [Host / HardRock]"
	private bool IsSlotStatusNotification(string banchoResponse) => banchoResponse.StartsWith("Slot ");
	private bool IsMatchActiveModsNotification(string banchoResponse) => banchoResponse.StartsWith("Active mods: ");
	private bool IsMatchModsUpdatedNotification(string banchoResponse) => banchoResponse.EndsWith("enabled FreeMod") || banchoResponse.EndsWith("disabled FreeMod");
	private bool IsPlayerFinishedNotification(string banchoResponse) => banchoResponse.Contains("finished playing (Score:");
	private bool IsPlayerLeftNotification(string banchoResponse) => banchoResponse.EndsWith(" left the game.");
	private bool IsAllPlayersReadyNotification(string banchoResponse) => banchoResponse.StartsWith("All players are ready");
	private bool IsMatchAbortedNotification(string banchoResponse) => banchoResponse.StartsWith("Aborted the match");
	
	private void UpdateName(string banchoResponse)
	{
		// Process room name

		// Index of where the multiplayer lobby name begins
		int index = banchoResponse.LastIndexOf(',');
		string nameSub = banchoResponse[..index];
		string name = nameSub.Split(':')[1].Trim();

		Name = name;
	}

	private MultiplayerPlayer? FindPlayer(string name) => Players.Find(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

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

	private async Task SendAsync(string command) => await _client.SendAsync($"PRIVMSG {ChannelName} {command}");

#region MultiplayerLobby update methods
	private void UpdateMatchHost(string banchoResponse)
	{
		string hostPlayerName = banchoResponse.Split("Changed match host to ")[1];
		string? prevHostName = Host?.Name;

		Host = FindPlayer(hostPlayerName);

		if (Host is not null)
		{
			if (Host.Name != prevHostName)
			{
				OnHostChanged?.Invoke(Host);
			}
		}
	}

	private void UpdatePlayerInformation(string banchoResponse)
	{
		// Find the digit(s) within the first 8 characters, which is the slot number
		int slot = int.Parse(banchoResponse[..8].Where(char.IsDigit).ToArray());

		// Find the first ' ' after the URL, since the URL is not padded with any spaces.
		int playerNameBegin = banchoResponse.IndexOf(' ', banchoResponse.IndexOf("/u/", StringComparison.Ordinal)) + 1;

		string playerName = banchoResponse.Substring(playerNameBegin, 16).TrimEnd();

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
			player = new MultiplayerPlayer(playerName, slot)
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
			player = new MultiplayerPlayer(name, slotNum);
			Players.Add(player);
		}
		
		int previousSlot = player!.Slot;
		player.Slot = slotNum;

		OnPlayerSlotMove?.Invoke(new PlayerSlotMoveEventArgs(player, previousSlot, slotNum));
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
			OnPlayerJoined?.Invoke(new MultiplayerPlayer(playerName, slotNum, color));
		}
		else
		{
			int slotNum = int.Parse(splits[1][..^1]); // Ignore trailing period
			OnPlayerJoined?.Invoke(new MultiplayerPlayer(playerName, slotNum));
		}
	}

	private void UpdateHostChangedBeatmap(string banchoResponse)
	{
		HostIsChangingMap = false;

		int lastSlashIdx = banchoResponse.LastIndexOf('/');
		string idSub = banchoResponse[(lastSlashIdx + 1)..^1];

		OnBeatmapChanged?.Invoke(new BeatmapShell(int.Parse(idSub), GameMode));
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

		WinCondition = ParseWinCondition(winCondition);
		Format = ParseFormat(format);
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
	}
#endregion
}