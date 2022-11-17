using BanchoSharp.Interfaces;
using BanchoSharp.Messaging;
using BanchoSharp.Multiplayer;

namespace BanchoSharpTests;

public class ClientTests
{
	[SetUp]
	public void Setup() {}

	[Test]
	public async Task TestJoinChannelAsync()
	{
		var client = new BanchoClient();
		string[] channels = { "#osu", "The Omy Nomy Does Not Exist" };

		foreach (string channel in channels)
		{
			await client.JoinChannelAsync(channel);

			Assert.Multiple(() =>
			{
				var ch = client.GetChannel(channel);
				Assert.That(ch, Is.Not.Null);

#pragma warning disable CS8602
				Assert.That(client.ContainsChannel(ch.ChannelName));
				Assert.That(ch.ChannelName, Is.EqualTo(channel));
#pragma warning restore CS8602
			});
		}
	}

	[Test]
	public async Task TestPartChannelAsync()
	{
		var client = new BanchoClient();

		await client.JoinChannelAsync("#osu");
		await client.JoinChannelAsync("#english");
		await client.JoinChannelAsync("#mp_128498");
		await client.JoinChannelAsync("Some random guy");
		await client.JoinChannelAsync("#some_random_guy");

		Assert.That(client.Channels.Count, Is.GreaterThan(0));

		foreach (var channel in client.Channels.ToList())
		{
			await client.PartChannelAsync(channel.ChannelName);
			Assert.That(client.GetChannel(channel.ChannelName), Is.Null);
		}
	}

	[Test]
	public async Task TestQueryUser()
	{
		var client = new BanchoClient();

		await client.QueryUserAsync("Stage");
		await client.QueryUserAsync("TheOmyNomy");
		await client.QueryUserAsync("Peach");
		await client.QueryUserAsync("Mario");

		Assert.That(client.Channels, Has.Count.EqualTo(4));

		foreach (var channel in client.Channels)
		{
			Assert.That(channel is {} chatCh && chatCh.ChannelName == channel.ChannelName);
		}
	}

	[Test]
	public async Task TestMessageHistory()
	{
		var client = new BanchoClient();

		IChatChannel[] channels = { new Channel("TheOmyNomy"), 
			new Channel("#osu"), new MultiplayerLobby(client, 123, "awesome tournament 5") };

		foreach (var channel in channels)
		{
			Assert.That(channel.MessageHistory, Is.Not.Null);
			await client.JoinChannelAsync(channel.ChannelName);
			await client.SendPrivateMessageAsync(channel.ChannelName, "Hello world");
		}

		foreach (var channel in client.Channels.ToList())
		{
			Assert.That(channel.MessageHistory!.Count, Is.GreaterThan(0));
		}
	}

	[Test]
	public async Task TestOnMessageSent()
	{
		var client = new BanchoClient();
		bool sent = false;
		
		client.OnPrivateMessageSent += _ => sent = true;
		await client.SendPrivateMessageAsync("Dummy", "Hello world");
		
		Assert.That(sent, Is.True);
	}
}