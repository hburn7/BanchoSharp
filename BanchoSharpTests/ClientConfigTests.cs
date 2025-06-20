using BanchoSharp.Interfaces;

namespace BanchoSharpTests;

public class ClientConfigTests
{
	private IBanchoClient _client;
	
	[SetUp]
	public void Setup()
	{
		_client = new BanchoClient();
	}

	[TearDown]
	public void TearDown()
	{
		_client?.Dispose();
	}

	[Test]
	public async Task TestSaveMessagesConfig()
	{
		var config = new BanchoClientConfig(new IrcCredentials());
		_client.ClientConfig = config;

		await _client.JoinChannelAsync("#osu");
		var channel = _client.GetChannel("#osu");
		Assert.That(channel is { MessageHistory: {} });
		
		// Test disabled config
		_client.ClientConfig = new BanchoClientConfig(new IrcCredentials(), LogLevel.Info, false);
		await _client.JoinChannelAsync("#english");
		var channel2 = _client.GetChannel("#english");
		Assert.That(channel2!.MessageHistory, Is.EqualTo(null));
	}
}