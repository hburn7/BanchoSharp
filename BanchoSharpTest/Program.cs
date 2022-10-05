// See https://aka.ms/new-console-template for more information

using BanchoSharp;
using BanchoSharp.Multiplayer;

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
	const string lobby = "#mp_104231398";

	Console.WriteLine("Authenticated");
	await client.JoinChannelAsync(lobby);

	var mp = new MultiplayerLobby(client, lobby, "some lobby");
	
	mp.OnLobbyTimerStarted += seconds => Console.WriteLine($"Timer started for {seconds}s.");
	mp.OnLobbyTimerFinished += () => Console.WriteLine("Timer finished.");
	mp.OnMatchStarted += () => Console.WriteLine("Match started.");
	mp.OnMatchFinished += () => Console.WriteLine("Match finished.");
	
	mp.OnSettingsUpdated += () =>
	{
		Console.WriteLine($"{mp.Name}");
	};

	await mp.SetTimerAsync(120);
	await mp.AbortTimerAsync();
	await mp.SetMatchStartTimerAsync(5);
	
	Console.WriteLine(mp.MatchInProgress);
};

client.OnChannelJoinFailure += name =>
{
	Console.WriteLine($"Failed to join {name}");
};




await client.ConnectAsync();

