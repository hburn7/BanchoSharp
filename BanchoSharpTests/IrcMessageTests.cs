using BanchoSharp.Interfaces;
using BanchoSharp.Messaging.ChatMessages;

namespace BanchoSharpTests;

public class IrcMessageTests
{
	private static IIrcMessage _sampleMessage => new IrcMessage(":cho.ppy.sh 001 Stage :Welcome to the osu!Bancho.");
	private static IPrivateIrcMessage _samplePrivateMessage => PrivateIrcMessage.CreateFromParameters("TheOmyNomy",
		"Stage", "Hello world!");
	private static IPrivateIrcMessage _sampleBanchoBotMessage => PrivateIrcMessage.CreateFromParameters("BanchoBot",
		"Stage", "w00t p00t");

	[SetUp]
	public void Setup() {}

	[Test]
	public void TestCreationTimestamp()
	{
		var copy = _sampleMessage;
		var dt = DateTime.Now;

		Assert.That(IsWithin1ms(copy.Timestamp, dt));
	}

	[Test]
	public void TestParseValidity()
	{
		string raw = _sampleMessage.RawMessage;
		var newMessage = new IrcMessage(raw);

		Assert.Multiple(() =>
		{
			Assert.That(raw, Is.EqualTo(newMessage.RawMessage));
			Assert.That(newMessage.Command, Is.EqualTo("001"));
			Assert.That(newMessage.Prefix, Is.EqualTo("cho.ppy.sh"));
			Assert.That(newMessage.Tags, Has.Count.EqualTo(0));
			Assert.That(newMessage.Parameters[0], Is.EqualTo("Stage"));
			Assert.That(newMessage.Parameters[1], Is.EqualTo("Welcome to the osu!Bancho."));
		});
	}

	[Test]
	public void TestPrivateIrcMessageCreatedFromParameters()
	{
		var copy = new PrivateIrcMessage(_samplePrivateMessage.RawMessage);
		Assert.Multiple(() =>
		{
			Assert.That(copy.Content, Is.EqualTo("Hello world!"));
			Assert.That(copy.Recipient, Is.EqualTo("Stage"));
			Assert.That(copy.Sender, Is.EqualTo("TheOmyNomy"));
			Assert.That(copy.IsDirect, Is.EqualTo(true));
			Assert.That(copy.IsBanchoBotMessage, Is.EqualTo(false));
			Assert.That(copy.Command, Is.EqualTo("PRIVMSG"));
		});
	}

	[Test]
	public void TestIsDirect() => Assert.That(_samplePrivateMessage.IsDirect);

	[Test]
	public void TestIsBanchoBotMessage() => Assert.Multiple(() =>
	{
		Assert.That(_sampleBanchoBotMessage.IsDirect);
		Assert.That(_sampleBanchoBotMessage.IsBanchoBotMessage);

		var invalid = PrivateIrcMessage.CreateFromParameters("NotBanchoBot", "DefinitelyNotBanchoBot", "Invalid!");
		Assert.That(invalid.IsDirect);
		Assert.That(!invalid.IsBanchoBotMessage);
	});

	[Test]
	public void TestPrivateIrcMessageConstructor()
	{
		// Random message pulled from #osu
		string raw = ":c4mmy!cho@ppy.sh PRIVMSG #osu :nah i mean gigachad lily";
		IPrivateIrcMessage priv = new PrivateIrcMessage(raw);
		Assert.Multiple(() =>
		{
			Assert.That(priv.Content, Is.EqualTo("nah i mean gigachad lily"));
			Assert.That(priv.Recipient, Is.EqualTo("#osu"));
			Assert.That(priv.Sender, Is.EqualTo("c4mmy"));
			Assert.That(priv.IsDirect, Is.EqualTo(false));
			Assert.That(priv.IsBanchoBotMessage, Is.EqualTo(false));
		});
	}

	private bool IsWithin1ms(DateTime a, DateTime b) => Math.Abs((a - b).TotalMilliseconds) < 1;
}