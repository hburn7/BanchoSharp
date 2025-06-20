using BanchoSharp.Interfaces;
using BanchoSharp.Messaging.ChatMessages;
using BanchoSharp.Multiplayer;
using System.Text;

namespace BanchoSharpTests;

public class MultiplayerTests
{
	private IBanchoClient _client;
	private IMultiplayerLobby _lobby;
	// Commands
	// private const string _mpSettings =
	// 	"Room name: 3.00* - 5.00* | Auto Host Rotate, History: https://osu.ppy.sh/mp/104803682\nBeatmap: https://osu.ppy.sh/b/2216531 Sayuri - Koukai no Uta (TV Size)\nTeam mode: HeadToHead, Win condition: Score\nActive mods: Freemod\nPlayers: 7\nSlot 1  Not Ready https://osu.ppy.sh/u/22547049 Player 1        [Host]\nSlot 2  Not Ready https://osu.ppy.sh/u/14607058 Player 2        [Hidden, HardRock]\nSlot 3  Not Ready https://osu.ppy.sh/u/28851081 Player 3        \nSlot 4  Not Ready https://osu.ppy.sh/u/27831164 Player 4        \nSlot 6  Not Ready https://osu.ppy.sh/u/18028068 PlayerNameLarge1[Hidden, HardRock]\nSlot 7  Not Ready https://osu.ppy.sh/u/22940358 Player 6        \nSlot 8  Not Ready https://osu.ppy.sh/u/11796227 Player 7";

	private string _playerJoined(string name, int slot, string? team)
	{
		var builder = new StringBuilder($"{name} joined in slot {slot}");
		if (team != null)
		{
			if (team.ToLower() is not "red" or "blue")
			{
				throw new InvalidOperationException($"Invalid team ({team}). Expected 'red' or 'blue'.");
			}

			builder.Append($" for team {team}");
		}

		builder.Append('.');
		return builder.ToString();
	}

	private string _slotChanged(string name, int slot) => $"{name} moved to slot {slot}";
	private IMultiplayerLobby DefaultLobby(IBanchoClient client) => new MultiplayerLobby(client, 1, "test");

	private void InvokeEventInvoker(string message) => ((IBanchoBotEventInvoker)_client.BanchoBotEvents)
		.ProcessMessage(PrivateIrcMessage.CreateFromParameters("BanchoBot", "DummyRecipient", message));

	private void Invoke(IIrcMessage message) => _client.SimulateMessageReceived(message);
	private void InvokeToLobby(string message) => Invoke(PrivateIrcMessage.CreateFromParameters("BanchoBot", _lobby.ChannelName, message));

	[SetUp]
	public void Setup()
	{
		_client = new BanchoClient();
		_lobby = DefaultLobby(_client);
	}

	[TearDown]
	public void TearDown()
	{
		_client?.Dispose();
	}

	[TestCase("OWC 2021: (United States) vs. (Germany)", "https://osu.ppy.sh/mp/106275696", 106275696,
		Mods.HalfTime | Mods.Freemod, 1096903, "Unmei no Dark Side -Rolling Gothic mix", "Kanpyohgo", "Satellite's Lunatic",
		LobbyFormat.TagCoop, WinCondition.Score, TeamColor.None, true, PlayerState.NotReady,
		8191845, "Stage", 1,
		Mods.Easy | Mods.Hidden | Mods.Relax | Mods.Flashlight | Mods.SpunOut,
		"Room name: OWC 2021: (United States) vs. (Germany), History: https://osu.ppy.sh/mp/106275696",
		"Beatmap: https://osu.ppy.sh/b/1096903 Kanpyohgo - Unmei no Dark Side -Rolling Gothic mix [Satellite's Lunatic]",
		"Team mode: TagCoop, Win condition: Score",
		"Active mods: HalfTime, Freemod",
		"Players: 1",
		"Slot 1  Not Ready https://osu.ppy.sh/u/8191845 Stage           [Host / Easy, Hidden, Relax, Flashlight, SpunOut]")]
	public void TestMpSettingsUpdates(string lobbyName, string historyUrl, int matchId, Mods lobbyMods,
		int beatmapId, string beatmapTitle, string beatmapArtist, string beatmapDifficulty,
		LobbyFormat format,
		WinCondition winCondition, TeamColor p1TeamColor, bool p1IsHost, PlayerState p1State,
		int p1Id,
		string p1Name, int p1Slot, Mods p1Mods, params string[] updates)
	{
		_lobby = new MultiplayerLobby(_client, matchId, lobbyName);

		foreach (string u in updates)
		{
			// Generates the !mp settings response for these parameters and calls it
			Invoke(PrivateIrcMessage.CreateFromParameters("BanchoBot", $"#mp_{_lobby.Id}", u));
		}

		Assert.Multiple(() =>
		{
			// Assert that all parameters are equal to the corresponding multiplayerlobby values
			Assert.That(_lobby.Name, Is.EqualTo(lobbyName));
			Assert.That(_lobby.HistoryUrl, Is.EqualTo(historyUrl));
			Assert.That(_lobby.Id, Is.EqualTo(matchId));
			Assert.That(_lobby.Mods, Is.EqualTo(lobbyMods));
			Assert.That(_lobby.CurrentBeatmap?.Id, Is.EqualTo(beatmapId));
			Assert.That(_lobby.CurrentBeatmap?.Title, Is.EqualTo(beatmapTitle));
			Assert.That(_lobby.CurrentBeatmap?.Artist, Is.EqualTo(beatmapArtist));
			Assert.That(_lobby.CurrentBeatmap?.Difficulty, Is.EqualTo(beatmapDifficulty));
			Assert.That(_lobby.Format, Is.EqualTo(format));
			Assert.That(_lobby.WinCondition, Is.EqualTo(winCondition));
			Assert.That(_lobby.Host?.Equals(_lobby.Players[0]), Is.EqualTo(p1IsHost));
			Assert.That(_lobby.Players[0].Team, Is.EqualTo(p1TeamColor));
			Assert.That(_lobby.Players[0].State, Is.EqualTo(p1State));
			Assert.That(_lobby.Players[0].Id, Is.EqualTo(p1Id));
			Assert.That(_lobby.Players[0].Name, Is.EqualTo(p1Name));
			Assert.That(_lobby.Players[0].Slot, Is.EqualTo(p1Slot));
			Assert.That(_lobby.Players[0].Mods, Is.EqualTo(p1Mods));
		});
	}

	[TestCase(1314987, "Camellia", @"Feelin Sky (Camellia's ""200step"" Self-remix)", "Ambivalence", null,
		@"Camellia - Feelin Sky (Camellia's ""200step"" Self-remix) [Ambivalence] (https://osu.ppy.sh/b/1314987)")]
	[TestCase(676065, "FLOOR LEGENDS -KAC 2012-", "KAC 2012 ULTIMATE MEDLEY -HISTORIA SOUND VOLTEX-", "NOVICE", null,
		@"FLOOR LEGENDS -KAC 2012- - KAC 2012 ULTIMATE MEDLEY -HISTORIA SOUND VOLTEX- [NOVICE] (https://osu.ppy.sh/b/676065)")]
	public void TestBeatmapChanged(int id, string artist, string title, string diff,
		GameMode? mode, string matchingInput)
	{
		var beatmap = new BeatmapShell(id, artist, title, diff, mode);

		_lobby.OnBeatmapChanged += shell => { Assert.That(shell, Is.EqualTo(beatmap)); };
	}

	[TestCase("Changed match mode to Osu", GameMode.osu)]
	[TestCase("Changed match mode to Taiko", GameMode.osuTaiko)]
	[TestCase("Changed match mode to CatchTheBeat", GameMode.osuCatch)]
	[TestCase("Changed match mode to OsuMania", GameMode.osuMania)]
	public void TestMatchModeChange(string banchoResponse, GameMode mode)
	{
		InvokeToLobby(banchoResponse);
		Assert.That(_lobby.GameMode, Is.EqualTo(mode));
	}
	
	[Test]
	public void TestMpSetResponseParser()
	{
		var resBuilder = new StringBuilder("Changed match settings to ");
		// [# slots, ]<format>, <win condition>
		string[] formats = { "HeadToHead", "TagCoop", "TeamVs", "TagTeamVs" };
		string[] winConditions = { "Score", "Accuracy", "Combo", "ScoreV2" };
		string[] slots = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16" };

		// Test format only
		foreach (string format in formats)
		{
			resBuilder.Append($"{format}");
			InvokeToLobby(resBuilder.ToString());
			Assert.That(_lobby.Format, Is.EqualTo((LobbyFormat)Enum.Parse(typeof(LobbyFormat), format)));
			resBuilder.Clear();
			resBuilder.Append("Changed match settings to ");
		}

		// Test format and wincondition only
		foreach (string format in formats)
		{
			foreach (string winCondition in winConditions)
			{
				resBuilder.Append($"{format}, {winCondition}");
				InvokeToLobby(resBuilder.ToString());
				Assert.Multiple(() =>
				{
					Assert.That(_lobby.Format, Is.EqualTo((LobbyFormat)Enum.Parse(typeof(LobbyFormat), format)));
					Assert.That(_lobby.WinCondition, Is.EqualTo((WinCondition)Enum.Parse(typeof(WinCondition), winCondition)));
				});

				resBuilder.Clear();
				resBuilder.Append("Changed match settings to ");
			}
		}

		// Test format, wincondition and slots
		foreach (string format in formats)
		{
			foreach (string winCondition in winConditions)
			{
				foreach (string slot in slots)
				{
					resBuilder.Append($"{slot} slots, {format}, {winCondition}");
					InvokeToLobby(resBuilder.ToString());
					Assert.Multiple(() =>
					{
						Assert.That(_lobby.Format, Is.EqualTo((LobbyFormat)Enum.Parse(typeof(LobbyFormat), format)));
						Assert.That(_lobby.WinCondition, Is.EqualTo((WinCondition)Enum.Parse(typeof(WinCondition), winCondition)));
						Assert.That(_lobby.Size, Is.EqualTo(int.Parse(slot)));
					});

					resBuilder.Clear();
					resBuilder.Append("Changed match settings to ");
				}
			}
		}
	}

	[TestCase("Timper changed to Red", "Timper", TeamColor.Red)]
	[TestCase("Timper changed to Blue", "Timper", TeamColor.Blue)]
	[TestCase("Moved Timper to team Red", "Timper", TeamColor.Red)]
	[TestCase("Moved Timper to team Blue", "Timper", TeamColor.Blue)]
	public void TestTeamSwap(string response, string player, TeamColor team)
	{
		_lobby.Players.Add(new MultiplayerPlayer(_lobby, "Timper", 1));

		var match = _lobby.FindPlayer("Timper");
		Assert.That(match, Is.Not.Null);

		bool playerChangedTeam = false;
		TeamColor? prevTeam = null;
		TeamColor? newTeam = null;
		_lobby.OnPlayerChangedTeam += e =>
		{
			playerChangedTeam = true;
			prevTeam = e.PreviousTeam;
			newTeam = e.Player.Team;
		};
		
		InvokeToLobby(response);
		Assert.Multiple(() =>
		{
			Assert.That(playerChangedTeam, Is.True);
			Assert.That(prevTeam, Is.EqualTo(TeamColor.None));
			Assert.That(newTeam, Is.EqualTo(team));
		});
	}

	[Test]
	public void TestBannedPlayer()
	{
		Assert.That(!_lobby.Players.Any());
		var dummy = new MultiplayerPlayer(_lobby, "ban_me", 1);
		
		_lobby.Players.Add(dummy);
		Assert.That(_lobby.Players.Any());
		
		InvokeToLobby("Banned ban_me from the match");
		
		Assert.That(!_lobby.Players.Any());
	}

	[Test]
	public void TestMpClearhost()
	{
		var dummy = new MultiplayerPlayer(_lobby, "test", 1);

		_lobby.Players.Add(dummy);
		Assert.That(_lobby.Host, Is.Null);

		InvokeToLobby("test became the host.");
		Assert.That(_lobby.Host, Is.EqualTo(dummy));

		InvokeToLobby("Cleared match host");
		Assert.That(_lobby.Host, Is.Null);
	}

	[Test]
	public void TestFormatUpdateResetsPlayerInfo()
    {
        InvokeToLobby("Changed match settings to TeamVs");
		Assert.That(_lobby.Format, Is.EqualTo(LobbyFormat.TeamVs));
		
		InvokeToLobby("Stage joined in slot 1 for team blue.");
		InvokeToLobby("Timper joined in slot 2 for team blue.");
		InvokeToLobby("Foo joined in slot 3 for team red.");
		InvokeToLobby("Bar joined in slot 4 for team blue.");
		InvokeToLobby("Baz joined in slot 5 for team red.");
		
		Assert.That(_lobby.Players.Count, Is.EqualTo(5));
        Assert.Multiple(() =>
        {
            Assert.That(_lobby.Players[0].Team, Is.EqualTo(TeamColor.Blue));
            Assert.That(_lobby.Players[1].Team, Is.EqualTo(TeamColor.Blue));
            Assert.That(_lobby.Players[2].Team, Is.EqualTo(TeamColor.Red));
            Assert.That(_lobby.Players[3].Team, Is.EqualTo(TeamColor.Blue));
            Assert.That(_lobby.Players[4].Team, Is.EqualTo(TeamColor.Red));
        });
        
        InvokeToLobby("Changed match settings to TagTeamVs");
        Assert.Multiple(() =>
        {
	        Assert.That(_lobby.Players[0].Team, Is.EqualTo(TeamColor.Blue));
	        Assert.That(_lobby.Players[1].Team, Is.EqualTo(TeamColor.Blue));
	        Assert.That(_lobby.Players[2].Team, Is.EqualTo(TeamColor.Red));
	        Assert.That(_lobby.Players[3].Team, Is.EqualTo(TeamColor.Blue));
	        Assert.That(_lobby.Players[4].Team, Is.EqualTo(TeamColor.Red));
        });
        
        InvokeToLobby("Changed match settings to HeadToHead");
        Assert.That(_lobby.Format, Is.EqualTo(LobbyFormat.HeadToHead));
        Assert.Multiple(() =>
        {
	        Assert.That(_lobby.Players[0].Team, Is.EqualTo(TeamColor.None));
	        Assert.That(_lobby.Players[1].Team, Is.EqualTo(TeamColor.None));
	        Assert.That(_lobby.Players[2].Team, Is.EqualTo(TeamColor.None));
	        Assert.That(_lobby.Players[3].Team, Is.EqualTo(TeamColor.None));
	        Assert.That(_lobby.Players[4].Team, Is.EqualTo(TeamColor.None));
        });
    }

    [Test]
	public void TestMpSettingsBeatmap()
	{
		string input = "Beatmap: https://osu.ppy.sh/b/2572163 Kurokotei - Galaxy Collapse";
		string input2 = "Beatmap: https://osu.ppy.sh/b/2907160 Silentroom - NULCTRL";

		var shell = new BeatmapShell(2572163, "Kurokotei", "Galaxy Collapse", null, _lobby.GameMode);
		var shell2 = new BeatmapShell(2907160, "Silentroom", "NULCTRL", null, _lobby.GameMode);

		Assert.Multiple(() =>
		{
			InvokeToLobby(input);
			Assert.That(_lobby.CurrentBeatmap, Is.EqualTo(shell));

			InvokeToLobby(input2);
			Assert.That(_lobby.CurrentBeatmap, Is.EqualTo(shell2));
		});
	}

	[Test]
	public void TestAllPlayersReady()
	{
		_lobby.Players.Add(new MultiplayerPlayer(_lobby, "Player 1", 1));
		_lobby.Players.Add(new MultiplayerPlayer(_lobby, "Player 2", 2));
		_lobby.Players.Add(new MultiplayerPlayer(_lobby, "Player 3", 3));
		_lobby.Players.Add(new MultiplayerPlayer(_lobby, "Player 4", 4));

		foreach (var player in _lobby.Players)
		{
			Assert.That(player.State, Is.EqualTo(PlayerState.NotReady));
		}

		_lobby.OnAllPlayersReady += () =>
		{
			foreach (var player in _lobby.Players)
			{
				Assert.That(player.State, Is.EqualTo(PlayerState.Ready));
			}
		};

		InvokeToLobby("All players are ready!");
	}

	[Test]
	public void TestHostChangingBeatmap()
	{
		string[] messages = { "Changed beatmap to", "Beatmap changed to:" };

		Assert.That(_lobby.HostIsChangingMap, Is.False);
		foreach (string msg in messages)
		{
			InvokeToLobby("Host is changing map...");
			Assert.That(_lobby.HostIsChangingMap, Is.True);

			try
			{
				InvokeToLobby(msg);
				Assert.That(_lobby.HostIsChangingMap, Is.False);
			}
			catch (Exception)
			{
				// This is expected as there is no beatmap ID, title, etc.
			}
		}
	}

	// This also gets executed when the host manually selects a map
	[TestCase(":BanchoBot!cho@ppy.sh PRIVMSG #mp_1 :Beatmap changed to: Various Artists - FINGER CONTROL MEGAPACK [Renard - With Me (LVL1)] (https://osu.ppy.sh/b/3593124)",
		3593124, "Various Artists", "FINGER CONTROL MEGAPACK", "Renard - With Me (LVL1)")]
	[TestCase(":BanchoBot!cho@ppy.sh PRIVMSG #mp_1 :Beatmap changed to: baker - For a Dead Girl+ [Collab Extra] (https://osu.ppy.sh/b/1444316)",
		1444316, "baker", "For a Dead Girl+", "Collab Extra")]
	[TestCase(":BanchoBot!cho@ppy.sh PRIVMSG #mp_1 :Beatmap changed to: THE ORAL CIGARETTES - Flower [Sakura] (https://osu.ppy.sh/b/1738018)",
		1738018, "THE ORAL CIGARETTES", "Flower", "Sakura")]
	[TestCase(":BanchoBot!cho@ppy.sh PRIVMSG #mp_1 :Beatmap changed to: TheFatRat - Mayday (feat. Laura Brehm) [[2B] Calling Out Mayday] (https://osu.ppy.sh/b/1605148)",
		1605148, "TheFatRat", "Mayday (feat. Laura Brehm)", "[2B] Calling Out Mayday")]
	[TestCase(
		":BanchoBot!cho@ppy.sh PRIVMSG #mp_1 :Beatmap changed to: Toby Fox - MEGALOVANIA (Camellia Remix) [Tocorn x Ciyus Miapah : Inevitable Demise] (https://osu.ppy.sh/b/2169346)",
		2169346, "Toby Fox", "MEGALOVANIA (Camellia Remix)", "Tocorn x Ciyus Miapah : Inevitable Demise")]
	public void TestMpSet(string message, int id, string artist, string title,
		string diff)
	{
		var irc = new PrivateIrcMessage(message);

		_client.SimulateMessageReceived(irc);

		Assert.Multiple(() =>
		{
			Assert.That(_lobby.CurrentBeatmap, Is.Not.Null);
			Assert.That(_lobby.CurrentBeatmap!.Equals(new BeatmapShell(id, artist, title, diff, _lobby.GameMode)));
		});
	}

	[TestCase("a")]
	[TestCase("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
	[TestCase("⍝ږÇ=壕֤Я~𫮒֕�ꊇي탇ݛ󙖇暤t󋹝񥯿2ޥ춸Мᇫ𸏩e鑨򍏕╕񗋙")] // Random UTF-8
	public void TestPlayerJoined(string name)
	{
		string joinStr = _playerJoined(name, 1, "Red");
		_lobby.OnPlayerJoined += player =>
		{
			Assert.Multiple(() =>
			{
				Assert.That(player.Name, Is.EqualTo(name));
				Assert.That(player.Slot, Is.EqualTo(1));
				Assert.That(player.Team, Is.EqualTo(TeamColor.Red));
			});
		};

		InvokeEventInvoker(joinStr);
	}

	[TestCase("Slot 1  No Map    https://osu.ppy.sh/u/11536421 zfr             ", false, PlayerState.NoMap)]
	[TestCase("Slot 1  Not Ready https://osu.ppy.sh/u/11536421 zfr             ", false, PlayerState.NotReady)]
	[TestCase("Slot 1  Ready https://osu.ppy.sh/u/11536421 zfr             ", false, PlayerState.Ready)]
	[TestCase("Slot 1  Not Ready https://osu.ppy.sh/u/11536421 zfr             [Host]", true, PlayerState.NotReady)]
	[TestCase("Slot 1  Ready https://osu.ppy.sh/u/11536421 zfr             [Host]", true, PlayerState.Ready)]
	[TestCase("Slot 1  No Map https://osu.ppy.sh/u/11536421 zfr             [Host]", true, PlayerState.NoMap)]
	public void TestPlayerState(string message, bool isHost, PlayerState state)
	{
		_lobby.Players.Add(new MultiplayerPlayer(_lobby, "zfr", 1));
		InvokeToLobby(message);
		var target = _lobby.FindPlayer("zfr");
		Assert.Multiple(() =>
		{
			Assert.That(target, Is.Not.Null);

			if (isHost)
			{
				Assert.That(_lobby.Host, Is.EqualTo(target));
			}

			Assert.That(target!.State, Is.EqualTo(state));
		});
	}

	[Test]
	public void TestDoubleTimeNightcore()
	{
		// :BanchoBot!cho@ppy.sh PRIVMSG #mp_106715183 :Active mods: DoubleTime, Nightcore
	}

	[Test]
	public void TestPlayerChangedSlot()
	{
		string player1 = "dummy1";
		string player2 = "dummy2";
		string player3 = "dummy3";

		_lobby.OnPlayerSlotMove += e =>
		{
			var playerMatch = _lobby.FindPlayer(e.Player.Name);
			Assert.Multiple(() =>
			{
				Assert.That(playerMatch, Is.Not.Null);
				Assert.That(playerMatch!.Slot, Is.EqualTo(e.NewSlot));
			});
		};

		InvokeEventInvoker(_playerJoined(player1, 1, null));
		InvokeEventInvoker(_playerJoined(player2, 2, null));
		InvokeEventInvoker(_playerJoined(player3, 3, null));

		InvokeEventInvoker(_slotChanged(player1, 5));
		InvokeEventInvoker(_slotChanged(player2, 16));
		InvokeEventInvoker(_slotChanged(player3, 2));
	}

	[Test]
	public void TestCreation()
	{
		string content = "Created the tournament match https://osu.ppy.sh/mp/104889872 test with spaces 9nd 4umber5";

		bool invoked = false;
		_client.BanchoBotEvents.OnTournamentLobbyCreated += lobby =>
		{
			invoked = true;
			Assert.Multiple(() =>
			{
				Assert.That(lobby.Name, Is.EqualTo("test with spaces 9nd 4umber5"));
				Assert.That(lobby.ChannelName, Is.EqualTo("#mp_104889872"));
				Assert.That(lobby.HistoryUrl, Is.EqualTo("https://osu.ppy.sh/mp/104889872"));
				Assert.That(_client.Channels.Contains(lobby));
			});
		};

		InvokeEventInvoker(content);
		Assert.That(invoked, Is.True);
	}

	[Test]
	public void TestFindPlayer()
	{
		var nullMatch = _lobby.FindPlayer("Foo");
		Assert.That(nullMatch, Is.Null);

		_lobby.Players.Add(new MultiplayerPlayer(_lobby, "Foo", 1, TeamColor.Red));
		var match = _lobby.FindPlayer("Foo");
		Assert.That(match, Is.Not.Null);
	}

	[Test]
	public void TestTargetableName()
	{
		_lobby.Players.Add(new MultiplayerPlayer(_lobby, "Foo bar", 1, TeamColor.Red));

		Assert.That(_lobby.Players.First().TargetableName().Equals("Foo_bar"));

		_lobby.Players.First().Id = 123;
		Assert.That(_lobby.Players.First().TargetableName().Equals("#123"));
	}

	[Test]
	public async Task TestMultiplayerSizeTracking()
	{
		Assert.That(_lobby.Players.Count, Is.EqualTo(0));
		Assert.That(_lobby.PlayerCount, Is.EqualTo(0));
		_lobby.Players.Add(new MultiplayerPlayer(_lobby, "Foo", 1, TeamColor.Blue));

		Assert.That(_lobby.Players.Count, Is.EqualTo(1));
		Assert.That(_lobby.PlayerCount, Is.EqualTo(1));
		_lobby.Players.Add(new MultiplayerPlayer(_lobby, "Foo2", 2, TeamColor.Blue));
		_lobby.Players.Add(new MultiplayerPlayer(_lobby, "Bar", 3, TeamColor.Red));

		Assert.That(_lobby.Players.Count, Is.EqualTo(3));
		Assert.That(_lobby.PlayerCount, Is.EqualTo(3));

		await _lobby.KickAsync(_lobby.FindPlayer("Foo")!);
		Assert.That(_lobby.Players.Count, Is.EqualTo(2));
		Assert.That(_lobby.PlayerCount, Is.EqualTo(2));
	}

	[Test]
	public async Task TestKickRemovesPlayerAsync()
	{
		_lobby.Players.Add(new MultiplayerPlayer(_lobby, "Foo", 1, TeamColor.Blue));
		_lobby.Players.Add(new MultiplayerPlayer(_lobby, "Bar", 2, TeamColor.Red));

		await _lobby.KickAsync(_lobby.FindPlayer("Foo")!);
		Assert.Multiple(() =>
		{
			Assert.That(_lobby.Players, Has.Count.EqualTo(1));
			Assert.That(_lobby.PlayerCount, Is.EqualTo(1));
			Assert.That(_lobby.Players.FirstOrDefault()?.Name, Is.EqualTo("Bar"));
			Assert.That(_lobby.Players.FirstOrDefault()?.Slot, Is.EqualTo(2));
		});

		await _lobby.KickAsync(_lobby.FindPlayer("Bar")!);
		Assert.Multiple(() =>
		{
			Assert.That(_lobby.Players, Is.Empty);
			Assert.That(_lobby.PlayerCount, Is.EqualTo(0));
		});
	}

	[Test]
	public async Task TestMultiplayerPlayerLobbyOwnership()
	{
		_lobby.Players.Add(new MultiplayerPlayer(_lobby, "Foo", 1, TeamColor.Blue));
		Assert.That(_lobby.Players.First().Lobby, Is.EqualTo(_lobby));

		await _lobby.KickAsync(_lobby.Players.First());
		Assert.That(_lobby.Players.Count == 0);
	}

	[Test]
	public void TestPlayerFinishedResults()
	{
		_lobby.Players.Add(new MultiplayerPlayer(_lobby, "Player 1", 1));
		_lobby.Players.Add(new MultiplayerPlayer(_lobby, "Player 2", 2));

		InvokeToLobby("Player 1 finished playing (Score: 7428260, PASSED).");
		InvokeToLobby("Player 2 finished playing (Score: 196409, FAILED).");

		Assert.Multiple(() =>
		{
			Assert.That(_lobby.FindPlayer("Player 1")?.Score, Is.EqualTo(7428260));
			Assert.That(_lobby.FindPlayer("Player 2")?.Score, Is.EqualTo(196409));

			Assert.That(_lobby.FindPlayer("Player 1")?.Passed, Is.True);
			Assert.That(_lobby.FindPlayer("Player 2")?.Passed, Is.False);
		});
	}
}