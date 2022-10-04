// See https://aka.ms/new-console-template for more information

using BanchoSharp;
using BanchoSharp.Multiplayer;
using System.Threading.Channels;

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

client.OnAuthenticated += async () =>
{
	Console.WriteLine("Authenticated");
	await client.JoinChannelAsync("#mp_104229446");

	var mp = new MultiplayerLobby(client, "#mp_104229446");
	await mp.DisplaySettingsAsync();
};

client.OnChannelJoinFailure += name =>
{
	Console.WriteLine($"Failed to join {name}");
};


await client.ConnectAsync();

