using BanchoSharp.Interfaces;
using BanchoSharp.Messaging;
using BanchoSharp.Messaging.ChatMessages;

namespace BanchoSharpTests;

public class MultiplayerTests
{
	private IBanchoClient _client;
	private IBanchoBotEventInvoker _invoker;
	private IBanchoBotEvents _events;
	
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
}