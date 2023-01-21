using BanchoSharp.Interfaces;
using BanchoSharp.Messaging;

namespace BanchoSharpTests;

public class SlashComandTests
{
	[Test]
	[TestCase("/join #osu")]
	[TestCase("/join #english")]
	[TestCase("/query TheOmyNomy")]
	[TestCase("/query Some_Valid_Username_With_Numbers_12345")]
	[TestCase("/part #osu")]
	[TestCase("/part #english")]
	[TestCase("/away")]
	[TestCase("/away some really silly message :3c")]
	[TestCase("/me some really silly message :3c")]
	[TestCase("/quit")]
	[TestCase("/clear")]
	[TestCase("/makelobby test lobby")]
	[TestCase("/logout")]
	public void TestCommand(string prompt)
    {
	    // Tests both valid and custom commands. All should be supported the same way.
        ISlashCommandHandler handler = new SlashCommandHandler(prompt);
        
		string cmd = prompt.Split("/")[1].Split()[0];
		Assert.That(handler.Command, Is.EqualTo(cmd));

		// If the prompt has no parameters, it must be null.
		// If the prompt has parameters but they are not relevant, the array must be empty.
		string[]? parameters;
		if (prompt.Split().Length > 1)
		{
			// Params provided

			parameters = cmd is "me" or "away" ? 
				new string[] { string.Join(" ", prompt.Split()[1..]) } : 
				prompt.Split()[1..];
		}
		else
		{
			// Params not provided
			parameters = null;
		}
		
        Assert.Multiple(() =>
        {
            Assert.That(handler.Command, Is.EqualTo(cmd));
            Assert.That(handler.Parameters, Is.EqualTo(parameters));
        });
    }

	[Test]
	[TestCase("/join")]
	[TestCase("/part")]
	[TestCase("/query")]
	public void TestEmptyParameters(string prompt)
    {
        // Commands without the correct arguments passed should return a null parameter array
        ISlashCommandHandler handler = new SlashCommandHandler(prompt);
        Assert.Multiple(() =>
        {
            Assert.That(handler.Parameters, Is.EqualTo(null));
            Assert.That(handler.Parameters, Is.EqualTo(null));
        });
    }

	[Test]
	[TestCase("/join #osu with other args")]
	[TestCase("/part #osu with other args")]
	[TestCase("/away I'm afk guys!")]
	[TestCase("/me Look at me, it's me!")]
	[TestCase("/ignore ReallyAnnoyingGuy12")]
	public void TestRelevantParameters(string prompt)
	{
		ISlashCommandHandler handler = new SlashCommandHandler(prompt);

		switch (handler.Command!.ToLower())
		{
			case "join": Assert.That(handler.Parameters, Is.EqualTo(new string[] { "#osu" }));
				break;
			case "part": Assert.That(handler.Parameters, Is.EqualTo(new string[] { "#osu" }));
				break;
			case "away": Assert.That(handler.Parameters, Is.EqualTo(new string[] { "I'm afk guys!" }));
				break;
			case "me": Assert.That(handler.Parameters, Is.EqualTo(new string[] { "Look at me, it's me!" }));
				break;
			case "ignore": Assert.That(handler.Parameters, Is.EqualTo(new string[] { "ReallyAnnoyingGuy12" }));
				break;
		}
	}
	
	[Test]
	[TestCase("/join #osu with other args")]
	[TestCase("/part #osu with other args")]
	[TestCase("/away I'm afk guys!")]
	[TestCase("/me Look at me, it's me!")]
	[TestCase("/ignore ReallyAnnoyingGuy12")]
	public void TestParameters(string prompt)
	{
		ISlashCommandHandler handler = new SlashCommandHandler(prompt);

		switch (handler.Command!.ToLower())
		{
			case "join": Assert.That(handler.Parameters, Is.EqualTo(new string[] { "#osu" }));
				break;
			case "part": Assert.That(handler.Parameters, Is.EqualTo(new string[] { "#osu" }));
				break;
			case "away": Assert.That(handler.Parameters, Is.EqualTo(new string[] { "I'm afk guys!" }));
				break;
			case "me": Assert.That(handler.Parameters, Is.EqualTo(new string[] { "Look at me, it's me!" }));
				break;
			case "ignore": Assert.That(handler.Parameters, Is.EqualTo(new string[] { "ReallyAnnoyingGuy12" }));
				break;
		}
	}
	
	[TestCase("/join #osu")]
	[TestCase("/part #osu")]
	[TestCase("/me I'm doing something")]
	[TestCase("/ignore SomeGuy")]
	[TestCase("/unignore SomeGuy")]
	[TestCase("/away I'm afk")]
	[TestCase("/query SomeGuy")]
	public void TestIsBanchoCommand(string prompt)
	{
		ISlashCommandHandler handler = new SlashCommandHandler(prompt);
		Assert.That(handler.IsBanchoCommand, Is.True);
	}

	[TestCase("/somecustomcommand")]
	[TestCase("/somecustomcommand with args")]
	[TestCase("/clear")]
	[TestCase("/chat user")]
	[TestCase("/queryt user")]
	[TestCase("/joinn #osu")]
	[TestCase("/leave #channel")]
	[TestCase("/unaway")]
	public void TestIsNotBanchoCommand(string prompt)
	{
		ISlashCommandHandler handler = new SlashCommandHandler(prompt);
		Assert.That(handler.IsBanchoCommand, Is.False);
	}
}