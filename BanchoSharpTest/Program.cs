// See https://aka.ms/new-console-template for more information

using BanchoSharp;

var client = new BanchoClient(new BanchoClientConfig(new IrcCredentials("Stage",
	Environment.GetEnvironmentVariable("IRC_PASS")), LogLevel.None));

client.OnConnected += () => Console.WriteLine("Connected");

client.OnMessageReceived += m =>
{
	Console.WriteLine(m.RawMessage);
};

client.OnDeploy += s =>
{
	if (!s.Contains("PASS"))
	{
		Console.WriteLine(s);
	}
};

client.OnAuthenticated += () => { Console.WriteLine("Authenticated"); };


await client.ConnectAsync();

await client.JoinChannelAsync("#osu");

