using BanchoSharp.EventArgs;
using BanchoSharp.Exceptions;
using BanchoSharp.Interfaces;
using BanchoSharp.Messaging;
using System.Diagnostics;

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
	private int _playersRemainingCount = 0;

	public MultiplayerLobby(IBanchoClient client, long id, string name) : base($"#mp_{id}")
	{
		_client = client;

		Id = id;
		Name = name;
		HistoryUrl = $"https://osu.ppy.sh/mp/{id}";
		Size = 1;
		GameMode = GameMode.osu;

		Format = LobbyFormat.HeadToHead;
		WinCondition = WinCondition.Score;

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

		OnPlayerJoined += player => { Players.Add(player); };

		OnPlayerDisconnected += disconnectedEventArgs => Players.Remove(disconnectedEventArgs.Player);

		// Task.Run(TimerWatcher).GetAwaiter().GetResult();
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
	public string? HistoryUrl { get; private set; }
	public int Size { get; private set; }
	public MultiplayerPlayer? Host { get; private set; }
	public bool HostIsChangingMap { get; private set; }
	public bool MatchInProgress { get; private set; }
	public bool IsLocked { get; private set; }
	public bool IsClosed { get; private set; }
	public TimeSpan? LobbyTimerRemaining => _lobbyTimerEnd?.Subtract(DateTime.Now);
	public TimeSpan? MatchTimerRemaining => _matchTimerEnd?.Subtract(DateTime.Now);
	public bool MatchStartTimerInProgress => MatchTimerRemaining > TimeSpan.FromSeconds(0);
	public bool LobbyTimerInProgress => LobbyTimerRemaining > TimeSpan.FromSeconds(0);
	public LobbyFormat Format { get; private set; }
	public WinCondition WinCondition { get; private set; }
	public GameMode GameMode { get; private set; }
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
		OnMatchAborted?.Invoke();
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

	// private Task TimerWatcher()
	// {
	// 	while (true)
	// 	{
	// 		if (_lobbyTimerEnd != null && _lobbyTimerEnd < DateTime.Now)
	// 		{
	// 			OnLobbyTimerFinished?.Invoke();
	// 			_lobbyTimerEnd = null;
	// 		}
	//
	// 		if (_matchTimerEnd != null && _matchTimerEnd < DateTime.Now)
	// 		{
	// 			OnMatchStartTimerFinished?.Invoke();
	// 			_matchTimerEnd = null;
	// 		}
	// 	}
	// }

	private void ResetLobbyTimer() => _lobbyTimerEnd = null;
	private void ResetMatchTimer() => _matchTimerEnd = null;

	private void UpdateLobbyFromBanchoBotSettingsResponse(string banchoBotResponse)
	{
		if (banchoBotResponse.StartsWith("Room name: "))
		{
			// Process room name and history

			// Index of where the multiplayer lobby name begins
			int index = banchoBotResponse.LastIndexOf(',');
			string nameSub = banchoBotResponse[..index];
			string name = nameSub.Split(':')[1].Trim();

			string historySub = banchoBotResponse[(index + 1)..];
			string history = historySub.Substring(historySub.IndexOf(':') + 2);

			Name = name;
			HistoryUrl = history;
		}
		else if (banchoBotResponse.StartsWith("Team mode: "))
		{
			// Update team mode and win condition

			// Index of where the team mode string begins
			int index = banchoBotResponse.LastIndexOf(',');

			string winConditionSub = banchoBotResponse[(index + 1)..];
			string winCondition = winConditionSub.Split(':')[1].Trim();

			string formatSub = banchoBotResponse[..index];
			string format = formatSub.Split(':')[1].Trim();

			WinCondition = ParseWinCondition(winCondition);
			Format = ParseFormat(format);
		}
		else if (banchoBotResponse.Equals("Host is changing map..."))
		{
			HostIsChangingMap = true;
			OnHostChangingMap?.Invoke();
		}
		else if (banchoBotResponse.StartsWith("Beatmap changed to: "))
		{
			HostIsChangingMap = false;

			int lastSlashIdx = banchoBotResponse.LastIndexOf('/');
			string idSub = banchoBotResponse[(lastSlashIdx + 1)..^1];

			OnBeatmapChanged?.Invoke(new BeatmapShell(int.Parse(idSub), GameMode));
		}
		else if (banchoBotResponse.StartsWith("Changed match host to "))
		{
			string host = banchoBotResponse.Split("Changed match host to ")[1];
			Host = FindPlayer(host);
		}
		else if (banchoBotResponse.Contains(" joined in slot "))
		{
			if (banchoBotResponse.Contains("for team"))
			{
				string[] splits = banchoBotResponse.Split(" joined in slot ");
				string playerName = splits[0];
				int slotNum = int.Parse(splits[1][..2].Trim()); // Ignore trailing period
				string team = banchoBotResponse.Split(" for team ")[1][..^1];
				var color = team == "blue" ? TeamColor.Blue : TeamColor.Red;
				OnPlayerJoined?.Invoke(new MultiplayerPlayer(playerName, slotNum, color));
			}
			else
			{
				string[] splits = banchoBotResponse.Split(" joined in slot ");
				string playerName = splits[0];
				int slotNum = int.Parse(splits[1][..^1]); // Ignore trailing period
				OnPlayerJoined?.Invoke(new MultiplayerPlayer(playerName, slotNum));
			}
		}
		else if (banchoBotResponse.Equals("Started the match"))
		{
			OnMatchStarted?.Invoke();
			MatchInProgress = true;
		}
		else if (banchoBotResponse.Equals("The match has finished!"))
		{
			OnMatchFinished?.Invoke();
			MatchInProgress = false;
		}
		else if (banchoBotResponse.Contains("moved to slot"))
		{
			string[] splits = banchoBotResponse.Split(" moved to slot ");
			string name = splits[0].Trim();
			string slot = splits[1].Trim();
			int slotNum = int.Parse(slot);

			var player = FindPlayer(name);
			int previousSlot = player!.Slot;
			player!.Slot = slotNum;

			OnPlayerSlotMove?.Invoke(new PlayerSlotMoveEventArgs(player, previousSlot, slotNum));
		}
		else if (banchoBotResponse.StartsWith("Players: "))
		{
			var playerCount = int.Parse(banchoBotResponse.Split("Players: ")[1]);

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
		else if (banchoBotResponse.StartsWith("Slot "))
		{
			// Find the digit(s) within the first 8 characters
			var slot = int.Parse((banchoBotResponse[..8].Where(c => char.IsDigit(c)).ToArray()));

			// Find the first ' ' after the URL, since the URL is not padded with any spaces.
			var playerNameBegin = banchoBotResponse.IndexOf(' ', banchoBotResponse.IndexOf("/u/")) + 1;

			var name = banchoBotResponse.Substring(playerNameBegin, 16).TrimEnd();
			var info = banchoBotResponse.Length > (playerNameBegin + 16) ? banchoBotResponse[(playerNameBegin + 16)..] : null;

			var player = FindPlayer(name);

			if (player is null)
			{
				OnPlayerJoined?.Invoke(new MultiplayerPlayer(name, slot, TeamColor.None));

				player = FindPlayer(name);
			}
			else
			{
				if (player.Slot != slot)
				{
					var previousSlot = player.Slot;

					player.Slot = slot;

					OnPlayerSlotMove?.Invoke(new PlayerSlotMoveEventArgs(player, previousSlot, slot));
				}
			}

			if (info != null && player is not null)
			{
				// Just finding words in this string feels like an easier approach at the moment, since the string provided
				// by bancho seems to be using both '/' and ',' as a separator at the same time, and I don't see any
				// benefits with working that out right now.
				
				if (info.Contains("Host"))
				{
					var prevHostName = Host?.Name;

					Host = player;

					if (Host is not null)
					{
						if (Host.Name != prevHostName)
						{
							OnHostChanged?.Invoke(Host);
						}
					}
				}
				
				if (info.Contains("Team "))
				{
					var prevTeam = player.Team;

					if (info.Contains("Team Blue") && player.Team != TeamColor.Blue)
					{
						player.Team = TeamColor.Blue;
						
						OnPlayerChangedTeam?.Invoke(new PlayerChangedTeamEventArgs(player, prevTeam));
					}
					
					if (info.Contains("Team Red") && player.Team != TeamColor.Red)
					{
						player.Team = TeamColor.Red;
						
						OnPlayerChangedTeam?.Invoke(new PlayerChangedTeamEventArgs(player, prevTeam));
					}
				}
				
				// Only attempt to find player mods if Freemod is enabled
				if ((Mods & Mods.Freemod) != 0)
				{
					player.Mods = Mods.None;	
					
					// Since all mods should be correctly named directly within the enum, we should just
					// be able to match strings here.
					foreach (Mods mod in Enum.GetValues(typeof(Mods)))
					{
						if (mod == Mods.None) continue;

						var modName = Enum.GetName(typeof(Mods), mod);

						if (modName == null) continue;
					
						if (info.Contains(modName))
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
			
			if (--_playersRemainingCount == 0)
			{
				OnSettingsUpdated?.Invoke();
			}
		}
		else if (banchoBotResponse.StartsWith("Changed match host to "))
		{
			var hostPlayerName = banchoBotResponse.Split("Changed match host to ")[1];
			var prevHostName = Host?.Name;

			Host = FindPlayer(hostPlayerName);

			if (Host is not null)
			{
				if (Host.Name != prevHostName)
				{
					OnHostChanged?.Invoke(Host);
				}
			}
		}
		else if (banchoBotResponse.StartsWith("Active mods: ") || banchoBotResponse.EndsWith(", disabled FreeMod")) // This part is a little messy
		{
			if (banchoBotResponse.StartsWith("Disabled all mods,"))
			{
				Mods = Mods.Freemod;
			}
			else
			{
				var modList = "";
			
				if (banchoBotResponse.EndsWith(", disabled FreeMod"))
					modList = banchoBotResponse.Split(", disabled FreeMod")[0].Trim().Substring(8);
				else
					modList = banchoBotResponse.Split("Active mods: ")[1].Trim();
			
				var mods = modList.Split(',').ToList();

				if (!mods.Any())
					mods.Add(modList);

				Mods = Mods.None;

				foreach (var modStr in mods)
				{
					if (Enum.TryParse(modStr, out Mods mod))
					{
						Mods |= mod;
					}
					else
					{
						Logger.Warn($"Failed to parse mod called: {modStr}");
					}
				}
			}
		}
		else if (banchoBotResponse.EndsWith(", enabled FreeMod"))
		{
			// At this point no other mods could be turned on "by default"
			Mods = Mods.Freemod;
		}
		else if (banchoBotResponse.Contains("finished playing (Score: "))
		{
			var playerName = banchoBotResponse[..banchoBotResponse.IndexOf(" finished playing (Score: ")];
			var player = FindPlayer(playerName);

			if (player is not null)
			{
				var resultStr = banchoBotResponse[(banchoBotResponse.IndexOf(" finished playing (Score: ") + 26)..];
				var score = int.Parse(resultStr.Split(',')[0]);

				player.Score = score;
				player.Passed = resultStr.Contains("PASSED");
			}
		}
	}

	private MultiplayerPlayer? FindPlayer(string name) => Players.Find(x => x == name);

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
}