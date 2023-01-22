using BanchoSharp.Interfaces;
using BanchoSharp.Messaging;
using BanchoSharp.Multiplayer;

namespace BanchoSharpTests;

public class ClientTests
{
	private IBanchoClient _client;

	[SetUp]
	public void Setup() => _client = new BanchoClient();

	[Test]
	public async Task TestJoinChannelAsync()
	{
		string[] channels = { "#osu", "The Omy Nomy Does Not Exist" };

		foreach (string channel in channels)
		{
			await _client.JoinChannelAsync(channel);

			Assert.Multiple(() =>
			{
				var ch = _client.GetChannel(channel);
				Assert.That(ch, Is.Not.Null);

#pragma warning disable CS8602
				Assert.That(_client.ContainsChannel(ch.ChannelName));
				Assert.That(ch.ChannelName, Is.EqualTo(channel.Replace(' ', '_')));
#pragma warning restore CS8602

				Assert.That(_client.Channels.All(x => !x.ChannelName.Contains(' ')));
			});
		}
	}

	[Test]
	public async Task TestPartChannelAsync()
	{
		await _client.JoinChannelAsync("#osu");
		await _client.JoinChannelAsync("#english");
		await _client.JoinChannelAsync("#mp_128498");
		await _client.JoinChannelAsync("Some random guy");
		await _client.JoinChannelAsync("#some_random_guy");

		Assert.That(_client.Channels.Count, Is.GreaterThan(0));

		foreach (var channel in _client.Channels.ToList())
		{
			await _client.PartChannelAsync(channel.ChannelName);
			Assert.That(_client.GetChannel(channel.ChannelName), Is.Null);
		}
	}

	[Test]
	public async Task TestQueryUser()
	{
		await _client.QueryUserAsync("Stage");
		await _client.QueryUserAsync("TheOmyNomy");
		await _client.QueryUserAsync("Peach");
		await _client.QueryUserAsync("Mario");

		Assert.That(_client.Channels, Has.Count.EqualTo(4));

		foreach (var channel in _client.Channels)
		{
			Assert.That(channel is {} chatCh && chatCh.ChannelName == channel.ChannelName);
		}
	}

	[Test]
	public async Task TestMessageHistory()
	{
		IChatChannel[] channels = { new Channel("TheOmyNomy", true), 
			new Channel("#osu", true), new MultiplayerLobby(_client, 123, "awesome tournament 5") };

		foreach (var channel in channels)
		{
			Assert.That(channel.MessageHistory, Is.Not.Null);
			await _client.JoinChannelAsync(channel.ChannelName);
			await _client.SendPrivateMessageAsync(channel.ChannelName, "Hello world");
		}

		foreach (var channel in _client.Channels.ToList())
		{
			Assert.That(channel.MessageHistory!.Count, Is.GreaterThan(0));
		}
	}

	[Test]
	public async Task TestInvalidMessageHistory()
	{
		_client.ClientConfig = new BanchoClientConfig(new IrcCredentials(), LogLevel.Debug, false);

		IChatChannel[] channels = { new Channel("TheOmyNomy", _client.ClientConfig.SaveMessages), 
			new Channel("#osu", _client.ClientConfig.SaveMessages), new MultiplayerLobby(_client, 123, "awesome tournament 5") };
		
		foreach (var channel in channels)
		{
			Assert.That(channel.MessageHistory, Is.Null);
			await _client.JoinChannelAsync(channel.ChannelName);
			Assert.DoesNotThrowAsync(async () =>
			{
				await _client.SendPrivateMessageAsync(channel.ChannelName, "Hello world");
			});
		}

		foreach (var channel in _client.Channels.ToList())
		{
			Assert.That(channel.MessageHistory, Is.Null);
		}
	}

	[Test]
	public async Task TestOnMessageSent()
	{
		bool sent = false;
		
		_client.OnPrivateMessageSent += _ => sent = true;
		await _client.SendPrivateMessageAsync("Dummy", "Hello world");
		
		Assert.That(sent, Is.True);
	}
}