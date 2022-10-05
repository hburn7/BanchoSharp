using BanchoSharp.Exceptions;
using BanchoSharp.Interfaces;
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

public class MultiplayerLobby : IMultiplayerLobby
{
	private readonly IBanchoClient _client;
	private DateTime? _lobbyTimerEnd;
	private DateTime? _matchTimerEnd;

	public MultiplayerLobby(IBanchoClient client, string channel)
	{
		if (!channel.StartsWith("#mp_"))
		{
			throw new IrcException("Multiplayer lobby channels must start with #mp_");
		}

		_client = client;

		Channel = channel;
		Size = 1;
		GameMode = GameMode.osu;

		Format = LobbyFormat.HeadToHead;
		WinCondition = WinCondition.Score;

		_client.OnMessageReceived += m =>
		{
			if (m is IPrivateMessage { Sender: "BanchoBot" } pm && pm.Recipient == channel)
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

		OnPlayerJoined += player =>
		{
			Players.Add(player);
		};

		OnPlayerDisconnected += player => Players.Remove(player);

		Task.Run(TimerWatcher).GetAwaiter().GetResult();
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
	public event Action<MultiplayerPlayer>? OnPlayerChangedTeam;
	public event Action<MultiplayerPlayer>? OnPlayerSlotMove;
	public event Action<MultiplayerPlayer>? OnPlayerDisconnected;
	public event Action? OnHostChangingMap;
	public string Channel { get; }
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

	public async Task StartAsync()
	{
		await SendAsync("!mp start");
	}

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

	public async Task UpdateAsync()
	{
		int count = 0;
		var sw = new Stopwatch();
		sw.Start();

		var trackSettingsMessages = delegate(IPrivateMessage message)
		{
			if (message.Sender == "BanchoBot" && message.Recipient == Channel)
			{
				UpdateLobbyFromBanchoBotSettingsResponse(message.Content);
				count++;
			}
		};

		_client.OnMessageReceived += m =>
		{
			if (m is IPrivateMessage dm)
			{
				trackSettingsMessages(dm);
			}
		};

		while (count < 3 && sw.ElapsedMilliseconds < 10000)
		{
			await Task.Delay(25);
		}

		if (sw.ElapsedMilliseconds >= 10000)
		{
			Logger.Warn("Multiplayer settings update watcher timed out");
		}

		OnSettingsUpdated?.Invoke();
	}

	private Task TimerWatcher()
	{
		while (true)
		{
			if (_lobbyTimerEnd != null && _lobbyTimerEnd < DateTime.Now)
			{
				OnLobbyTimerFinished?.Invoke();
				_lobbyTimerEnd = null;
			}

			if (_matchTimerEnd != null && _matchTimerEnd < DateTime.Now)
			{
				OnMatchStartTimerFinished?.Invoke();
				_matchTimerEnd = null;
			}
		}
	}

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
			player!.Slot = slotNum;
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

	private async Task SendAsync(string command) => await _client.SendAsync($"PRIVMSG {Channel} {command}");
}