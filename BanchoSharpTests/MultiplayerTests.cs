using BanchoSharp.Interfaces;
using BanchoSharp.Messaging;
using BanchoSharp.Messaging.ChatMessages;
using BanchoSharp.Multiplayer;
using System.Text;

namespace BanchoSharpTests;

public class MultiplayerTests
{
	// Commands
	private const string _mpSettings =
		"Room name: 3.00* - 5.00* | Auto Host Rotate, History: https://osu.ppy.sh/mp/104803682\nBeatmap: https://osu.ppy.sh/b/2216531 Sayuri - Koukai no Uta (TV Size)\nTeam mode: HeadToHead, Win condition: Score\nActive mods: Freemod\nPlayers: 7\nSlot 1  Not Ready https://osu.ppy.sh/u/22547049 Player 1        [Host]\nSlot 2  Not Ready https://osu.ppy.sh/u/14607058 Player 2        [Hidden, HardRock]\nSlot 3  Not Ready https://osu.ppy.sh/u/28851081 Player 3        \nSlot 4  Not Ready https://osu.ppy.sh/u/27831164 Player 4        \nSlot 6  Not Ready https://osu.ppy.sh/u/18028068 PlayerNameLarge1[Hidden, HardRock]\nSlot 7  Not Ready https://osu.ppy.sh/u/22940358 Player 6        \nSlot 8  Not Ready https://osu.ppy.sh/u/11796227 Player 7        ";

	// private string _mpSettings(string lobbyName, int lobbyId, string curBeatmapName, int curBeatmapId,
	// 	LobbyFormat curFormat, WinCondition winCondition, Mods activeMods, params MultiplayerPlayer[] players)
	// {
	// 	var builder = new StringBuilder($"Room name: {lobbyName}, History: https://osu.ppy.sh/mp/{lobbyId}")
	// 	              .AppendLine($"Beatmap: https://osu.ppy.sh/b/{curBeatmapId} {curBeatmapName}")
	// 	              .AppendLine($"Team mode: {curFormat}, Win condition: {winCondition}")
	// 	              .AppendLine($"Active mods: {activeMods.ToAbbreviatedForm()}");
	// }
	
	private const string _mpStart = "Started the match";
	private const string _mpAbort = "Aborted the match";
	
	private IBanchoClient _client;
	private IBanchoBotEvents _events;
	private IBanchoBotEventInvoker _invoker;
	private string _mpHost(string name) => $"Changed match host to {name}";
	private string _mpMap(string id) => $"Changed beatmap to https://osu.ppy.sh/b/{id} EXAMPLE ARTIST - EXAMPLE TITLE";

	// Events
	private const string _matchStarted = "The match has started!";
	private const string _matchFinished = "The match has finished!";
	
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

	private string _teamChanged(string name, int slot) => $"{name} moved to slot {slot}";
	private string _playerLeft(string name) => $"{name} left the game.";

	private string _matchFinishedPlayerStats(string name, int score, bool passed) => new StringBuilder($"{name} finished playing (Score: {score}, ")
	                                                                                 .Append(passed ? "PASSED" : "FAILED")
	                                                                                 .Append(").")
	                                                                                 .ToString();

	private string _hostChanged(string name) => $"{name} became the host.";
	private string _beatmapChanged(string title, string diff, int id) => $"Beatmap changed to: {title} [{diff}] (https://osu.ppy.sh/b/{id})";

	[SetUp]
	public void Setup()
	{
		_client = new BanchoClient();
		_invoker = new BanchoBotEventInvoker(_client);
		_events = (IBanchoBotEvents)_invoker;
	}

	[Test]
	public void TestCreation()
	{
		string content = "Created the tournament match https://osu.ppy.sh/mp/104889872 test with spaces 9nd 4umber5";
		var msg = PrivateIrcMessage.CreateFromParameters("BanchoBot", "Dummy", content);

		bool invoked = false;
		_events.OnTournamentLobbyCreated += lobby =>
		{
			invoked = true;
			Assert.Multiple(() =>
			{
				Assert.That(lobby.Name, Is.EqualTo("test with spaces 9nd 4umber5"));
				Assert.That(lobby.ChannelName, Is.EqualTo("#mp_104889872"));
				Assert.That(lobby.HistoryUrl, Is.EqualTo("https://osu.ppy.sh/mp/104889872"));
			});
		};

		_invoker.ProcessMessage(msg);
		Assert.That(invoked, Is.True);
	}

	[Test]
	public void TestMpSettings()
	{
		
	}
}