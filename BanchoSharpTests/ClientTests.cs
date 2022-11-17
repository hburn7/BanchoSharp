using BanchoSharp.Interfaces;
using BanchoSharp.Messaging;
using BanchoSharp.Multiplayer;

namespace BanchoSharpTests;

public class ClientTests
{
	[SetUp]
	public void Setup() {}

	[Test]
	public void TestJoinChannel()
	{
		var client = new BanchoClient();
		string[] channels = { "#osu", "The Omy Nomy Does Not Exist" };

		foreach (string channel in channels)
		{
			client.JoinChannelAsync(channel).GetAwaiter().GetResult();

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
	public void TestPartChannel()
	{
		var client = new BanchoClient();

		client.JoinChannelAsync("#osu").GetAwaiter().GetResult();
		client.JoinChannelAsync("#english").GetAwaiter().GetResult();
		client.JoinChannelAsync("#mp_128498").GetAwaiter().GetResult();
		client.JoinChannelAsync("Some random guy").GetAwaiter().GetResult();
		client.JoinChannelAsync("#some_random_guy").GetAwaiter().GetResult();

		Assert.That(client.Channels.Count, Is.GreaterThan(0));

		foreach (var channel in client.Channels.ToList())
		{
			client.PartChannelAsync(channel.ChannelName).GetAwaiter().GetResult();
			Assert.That(client.GetChannel(channel.ChannelName), Is.Null);
		}
	}

	[Test]
	public void TestQueryUser()
	{
		var client = new BanchoClient();

		client.QueryUserAsync("Stage").GetAwaiter().GetResult();
		client.QueryUserAsync("TheOmyNomy").GetAwaiter().GetResult();
		client.QueryUserAsync("Peach").GetAwaiter().GetResult();
		client.QueryUserAsync("Mario").GetAwaiter().GetResult();

		Assert.That(client.Channels, Has.Count.EqualTo(4));

		foreach (var channel in client.Channels)
		{
			Assert.That(channel is IChatChannel chatCh && chatCh.ChannelName == channel.ChannelName);
		}
	}

	[Test]
	public void TestMessageHistory()
	{
		var client = new BanchoClient();

		IChatChannel[] channels = { new Channel("TheOmyNomy"), 
			new Channel("#osu"), new MultiplayerLobby(client, 123, "awesome tournament 5") };

		foreach (var channel in channels)
		{
			Assert.That(channel.MessageHistory, Is.Not.Null);
			client.JoinChannelAsync(channel.ChannelName).GetAwaiter().GetResult();
			client.SendPrivateMessageAsync(channel.ChannelName, "Hello world").GetAwaiter().GetResult();
		}

		foreach (var channel in client.Channels.ToList())
		{
			Assert.That(channel.MessageHistory!.Count, Is.GreaterThan(0));
		}
	}

	[Test]
	public void TestOnMessageSent()
	{
		var client = new BanchoClient();
		bool sent = false;
		
		client.OnPrivateMessageSent += _ => sent = true;
		client.SendPrivateMessageAsync("Dummy", "Hello world").GetAwaiter().GetResult();
		
		Assert.That(sent, Is.True);
	}
}