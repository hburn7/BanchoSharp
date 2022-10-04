using BanchoSharp.Exceptions;
using BanchoSharp.Interfaces;

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

public class MultiplayerLobby : IMultiplayerLobby
{
	private readonly IBanchoClient _client;

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
	}

	public string Channel { get; }
	public string Name { get; private set; }
	public string? HistoryUrl { get; private set; }
	public int Size { get; }
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

	public async Task AbortAsync() => await SendAsync("!mp abort");
	public async Task AbortTimerAsync() => await SendAsync("!mp aborttimer");
	public async Task DisplaySettingsAsync() => await SendAsync("!mp settings");
	public async Task SetSizeAsync(int newSize) => await SendAsync($"!mp size {newSize}");
	public async Task MoveAsync(string player, int slot) => await SendAsync($"!mp move {player} {slot}");
	public async Task RenameAsync(string newName) => await SendAsync($"!mp name {newName}");
	public async Task InviteAsync(string username) => await SendAsync($"!mp invite {username}");
	public async Task LockAsync() => await SendAsync("!mp lock");
	public async Task UnlockAsync() => await SendAsync("!mp unlock");
	public async Task CloseAsync() => await SendAsync("!mp close");
	public async Task ClearHostAsync() => await SendAsync("!mp clearhost");
	public async Task SetHostAsync(string username) => await SendAsync($"!mp host {username}");
	public async Task SetModsAsync(params string[] mods) => await SendAsync($"!mp mods {string.Join(" ", mods)}");
	public async Task SetModsAsync(string mods) => await SendAsync($"!mp mods {mods}");
	public async Task SetTimerAsync(int seconds) => await SendAsync($"!mp timer {seconds}");
	public async Task SetStartTimerAsync(int seconds) => await SendAsync($"!mp start {seconds}");
	public async Task StartAsync() => await SendAsync("!mp start");
	public async Task KickAsync(string username) => await SendAsync($"!mp kick {username}");
	public async Task BanAsync(string username) => await SendAsync($"!mp ban {username}");
	public async Task AddRefereeAsync(params string[] usernames) => await SendAsync($"!mp addref {string.Join(" ", usernames)}");
	public async Task RemoveRefereesAsync(params string[] usernames) => await SendAsync($"!mp removeref {string.Join(" ", usernames)}");

	public async Task SetMapAsync(int id, GameMode? gameMode = null)
	{
		int modeNum;
		if (gameMode.HasValue)
		{
			modeNum = (int)gameMode.Value;
		}
		else
		{
			modeNum = (int)GameMode;
		}

		await SendAsync($"!mp map {id} {modeNum}");
	}

	public async Task SendHelpMessageAsync() => await SendAsync("!mp help");

	public async Task UpdateAsync()
	{
		int count = 0;

		Action<IPrivateMessage> trackSettingsMessages = delegate(IPrivateMessage message)
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

		while (count < 3)
		{
			await Task.Delay(100);
		}
	}

	private void UpdateLobbyFromBanchoBotSettingsResponse(string banchoBotResponse)
	{
		if (banchoBotResponse.StartsWith("Room"))
		{
			// Process room name and history

			// Index of where the multiplayer lobby name begins
			int index = banchoBotResponse.LastIndexOf(',');
			string nameSub = banchoBotResponse[..^index];
			string name = nameSub.Split(':')[1];

			string historySub = banchoBotResponse[(index + 1)..];
			string history = historySub.Split(':')[1];

			Name = name;
			HistoryUrl = history;
		}
		else if (banchoBotResponse.StartsWith("Team"))
		{
			// Update team mode and win condition

			// Index of where the team mode string begins
			int index = banchoBotResponse.LastIndexOf(',');

			string winConditionSub = banchoBotResponse[(index + 1)..];
			string winCondition = winConditionSub.Split(':')[1].Trim();

			string formatSub = banchoBotResponse[..^index];
			string format = formatSub.Split(':')[1].Trim();

			WinCondition = ParseWinCondition(winCondition);
			Format = ParseFormat(format);
		}
	}

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